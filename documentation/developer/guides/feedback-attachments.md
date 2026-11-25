# Feedback attachments

The feedback form accepts files. This is the template's first file feature. Later file
features should follow the same shape.

## Backing store

Azure Blob Storage is the real byte store. Cloudflare R2 is not used: the template already
deploys to Azure, and an S3 client would be a second object stack nothing else uses.

The byte store is chosen Azure, then Postgres, then a stand-in:

| `FeedbackAttachments:ConnectionString` | Postgres connection | Byte store |
| --- | --- | --- |
| set | any | `AzureFeedbackAttachmentBlobStore` |
| empty or missing | set | `EfFeedbackAttachmentBlobStore` (`feedback.feedback_attachment_bytes`, `bytea`) |
| empty or missing | empty or missing | `UnavailableFeedbackStorage` (reads empty, writes refused) |

The Postgres connection is `PostgresDbOptions:ConnectionString`, or the Aspire-injected
connection string for the database resource.

`FeedbackModule` registers `UnavailableFeedbackStorage` for the row ports. For the byte port
it registers Azure when Azure is configured, and the same stand-in otherwise. `PostgresDbModule` runs after it. When a
Postgres connection is set, it replaces the row ports with the EF stores, and the byte port
with the Postgres byte store unless Azure took it. There is no in-memory store (task 522). The
feedback slice compiles only with the `postgres` template flag, so an app generated without a
database has no feedback feature. With the flag on and no Postgres connection, the row ports
stay `UnavailableFeedbackStorage`: reads return no items and no attachments, removes do
nothing, and filing, updating, or uploading throws `InvalidOperationException` ("Feedback
storage is not available on this host. Configure a Postgres connection string."). The byte port
is the same stand-in unless Azure is configured. The stand-in keeps Development's build-time DI
validation passing.

`FeedbackAttachments:ContainerName` defaults to `feedback-attachments`. The name must be 3–63
characters, lowercase letters, digits, and single hyphens, and must not start or end with a
hyphen. The container is private and is created on first use. AppHost does not add Azurite.

Set the connection string in the environment as `FeedbackAttachments__ConnectionString`. The
committed `appsettings.json` leaves it empty, so a host with Postgres keeps the bytes in the
database.

The Postgres store keeps the bytes beside the rows, in its own table keyed by storage key.
Upload writes the bytes before the row, and remove deletes the row before the bytes, so a
row always has bytes. Reads load the column (at most 5 MiB) into a `MemoryStream` rather than
streaming from the reader.

A row whose bytes are gone (uploaded before task 352, when the bytes were held in memory) is
left in place. Its download is HTTP 404 with the title "Attachment no longer available", after
the normal access check. `GetFeedback` and `GetFeedbackForAdmin` (and every admin write that
returns the admin view) set `IsFilePresent` on each attachment from the byte store's
`ExistsAsync`, so the item pages show the file name with "file no longer stored" instead of a
broken image or a link that 404s, in the attachment list and where the body shows that file.
The flag is read from the byte store on every read, not stored on the row, so it cannot
disagree with the bytes and needs no reconcile job.

Object keys are `{owner:N}/{attachment:N}` with no leading slash.

## Limits

| Limit | Value |
| --- | --- |
| Max size | 5 MiB (`FeedbackAttachmentRules.MaxBytes`) |
| Files per item, and pending files per person | 8 |
| Pending lifetime | 24 hours (`FeedbackAttachmentRules.PendingLifetime`) |
| File name | 200 characters after the last path segment |
| Types | `image/png`, `image/jpeg`, `image/gif`, `image/webp`, `application/pdf`, `text/plain` |

SVG and HTML are refused. The server checks magic bytes, not only the claimed type. Oversize
is HTTP 413. A disallowed type or a body that does not match the type is HTTP 415. An empty
body is HTTP 400.

A pending file is one that was uploaded but not yet filed. The list of pending files lives
only in the browser, so a reload loses it. Each upload first deletes the caller's own pending
files older than the pending lifetime, rows and bytes, and then checks the cap of 8. A person
who abandons a draft is locked out of new uploads for at most one pending lifetime. A ninth
pending upload inside that window is HTTP 409. If an expired file's bytes cannot be deleted,
the failure is logged and the upload still succeeds.

A draft left open longer than the pending lifetime loses its earlier uploads the same way.
Submit then returns HTTP 400 and lists those ids under the `unavailableAttachmentIds` problem
extension. The form removes those files, their previews, and their markdown links, and shows
a message asking the user to attach the file again. The next submit files the rest. Concurrent uploads from one person can pass
the cap by a few files; the cap limits abuse and is not an exact quota.

The contract constants and the domain constants are duplicates. A test locks them together.
Contracts do not reference the domain.

## HTTP

`POST api/feedback/attachments` sends the file as the body, not JSON. The media type is the
file's type, with no charset. The file name is the percent-encoded `X-File-Name` header.
`HttpApiService` does this for any `IFileUploadRequest`. The server decodes the header and
keeps only the last path segment. A name with a control character, such as a decoded line
break, is HTTP 400. The endpoint accepts each allow-list media type. A POST otherwise accepts
only `application/json`, and the host answers an empty HTTP 415 before the binder runs. The
JSON default is cleared by a convention that removes every accepts entry, so the allow-list is
added after that convention. A type outside the list is that empty 415. A body that does not
match an accepted type is the handler's `application/problem+json` 415. The endpoint's request
body limit is one byte over the cap, so an
oversize body gets the handler's `application/problem+json` 413. A body that Kestrel stops
first gets the same 413 problem.

`GET api/feedback/attachments/{id}` writes the bytes. Images use `Content-Disposition: inline`.
PDF and text use `attachment`. `filename=` is an ASCII fallback in which every character
outside printable ASCII is `_`. The real name is in `filename*=UTF-8''…`. The response sets `X-Content-Type-Options: nosniff` and
`Cache-Control: private, no-store`. The `Content-Type` is the allow-list spelling.

An unlinked file can be downloaded or removed only by the person who uploaded it. After it is
filed, the item's owner or a principal with `admin.feedback.read` on `identity-session` or
`mock-identity-session` can download it. Anyone else gets the same 404 as a missing id.
Removing a filed file is HTTP 409.

Filing links each attachment with a conditional write that succeeds only when the row is
still the filer's and still pending. If a remove or a second submit takes a file first, every
link to the new item is undone, the item is removed, and the filer gets HTTP 400 listing that
id. If a store throws, the same rollback runs and the original error is kept. Once the item is
saved, a cancelled request does not stop linking or rollback partway. A successful rollback
leaves no item with only some of its files. If a rollback step itself throws, the failure is
logged at Error and the item can stay filed, possibly with some of its files; that needs two
store failures in a row.

`DELETE api/feedback/attachments/{id}` is the generated JSON endpoint. Upload and download are
hand-written endpoints because the generated one always writes JSON.

## Client

The file picker and the paste hook both call `UploadFeedbackAttachment`. Paste is a small
module on the Details box (`feedback-paste.ts`). Fluent UI v5 renders that box as
`fluent-textarea`. The module treats the element the page passes as that control when it
is one, and otherwise searches its descendants. It binds the paste on the control and on
the textarea in its open shadow root, including when that textarea appears after register.
It refuses an image over the size limit before reading it. It passes the file to .NET as a
JS stream reference, so paste also works on the InteractiveServer circuit of a first
InteractiveAuto visit. A refused or failed paste shows the page's normal error notification.
A failed upload shows the API problem. When that problem is missing, or is only the generic
unhandled error from an empty failure body, the form says the file could not be uploaded. A
successful upload appends a markdown link when it fits in the body. The item page lists the stored files and shows an image when the
type is `image/*`. The body stays plain text.
