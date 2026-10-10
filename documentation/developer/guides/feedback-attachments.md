# Feedback attachments

The feedback form accepts files. This is the template's first file feature. Later file
features should follow the same shape.

## Backing store

Azure Blob Storage is the real byte store. Cloudflare R2 is not used: the template already
deploys to Azure, and an S3 client would be a second object stack nothing else uses.

`FeedbackAttachmentBlobModule` chooses the implementation:

| `FeedbackAttachments:ConnectionString` | Store |
| --- | --- |
| empty or missing | `InMemoryFeedbackAttachmentBlobStore` (tests, mock mode, unconfigured hosts) |
| set | `AzureFeedbackAttachmentBlobStore` |

`FeedbackAttachments:ContainerName` defaults to `feedback-attachments`. The name must be 3–63
characters, lowercase letters, digits, and single hyphens, and must not start or end with a
hyphen. The container is private and is created on first use. AppHost does not add Azurite.

Set the connection string in the environment as `FeedbackAttachments__ConnectionString`. The
committed `appsettings.json` leaves it empty so a developer host stays in memory.

In-memory bytes are lost on restart. When the in-memory blob store runs next to the Postgres
row store, or in any environment other than Development, the host logs a startup warning:
the rows survive a restart but every older download returns 404. Set the connection string on
any deployed host.

Rows are separate from bytes. `IFeedbackAttachmentStore` is in-memory until Postgres is
configured, then `EfFeedbackAttachmentStore`. The blob registration does not follow the
Postgres flag.

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

`POST api/Feedback/attachments` sends the file as the body, not JSON. The media type is the
file's type, with no charset. The file name is the percent-encoded `X-File-Name` header.
`HttpApiService` does this for any `IFileUploadRequest`. The server decodes the header and
keeps only the last path segment. A name with a control character, such as a decoded line
break, is HTTP 400. The endpoint's request body limit is one byte over the cap, so an
oversize body gets the handler's `application/problem+json` 413. A body that Kestrel stops
first gets the same 413 problem.

`GET api/Feedback/attachments/{id}` writes the bytes. Images use `Content-Disposition: inline`.
PDF and text use `attachment`. `filename=` is an ASCII fallback in which every character
outside printable ASCII is `_`. The real name is in `filename*=UTF-8''…`. The response sets `X-Content-Type-Options: nosniff` and
`Cache-Control: private, no-store`. The `Content-Type` is the allow-list spelling.

An unlinked file can be downloaded or removed only by the person who uploaded it. After it is
filed, the item's owner or a principal with `admin.access` on `identity-session` or
`mock-identity-session` can download it. Anyone else gets the same 404 as a missing id.
Removing a filed file is HTTP 409.

Filing links each attachment with a conditional write that succeeds only when the row is
still the filer's and still pending. If a remove or a second submit takes a file first, every
link to the new item is undone, the item is removed, and the filer gets HTTP 400 listing that
id. If a store throws, the same rollback runs and the original error is kept. Once the item is
saved, a cancelled request does not stop linking or rollback partway. An item never keeps only
some of its files.

`DELETE api/Feedback/attachments/{id}` is the generated JSON endpoint. Upload and download are
hand-written endpoints because the generated one always writes JSON.

## Client

The file picker and the paste hook both call `UploadFeedbackAttachment`. Paste is a small
module on the Details box (`feedback-paste.ts`). It refuses an image over the size limit before
reading it. It passes the file to .NET as a JS stream reference, so paste also works on the
InteractiveServer circuit of a first InteractiveAuto visit. A refused or failed paste shows the
page's normal error notification. A successful upload appends a markdown link
when it fits in the body. The item page lists the stored files and shows an image when the
type is `image/*`. The body stays plain text.
