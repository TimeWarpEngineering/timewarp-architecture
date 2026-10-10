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

Rows are separate from bytes. `IFeedbackAttachmentStore` is in-memory until Postgres is
configured, then `EfFeedbackAttachmentStore`. The blob registration does not follow the
Postgres flag.

Object keys are `{owner:N}/{attachment:N}` with no leading slash.

## Limits

| Limit | Value |
| --- | --- |
| Max size | 5 MiB (`FeedbackAttachmentRules.MaxBytes`) |
| Files per item, and pending files per person | 8 |
| File name | 200 characters after the last path segment |
| Types | `image/png`, `image/jpeg`, `image/gif`, `image/webp`, `application/pdf`, `text/plain` |

SVG and HTML are refused. The server checks magic bytes, not only the claimed type. Oversize
is HTTP 413. A disallowed type or a body that does not match the type is HTTP 415. An empty
body is HTTP 400.

The contract constants and the domain constants are duplicates. A test locks them together.
Contracts do not reference the domain.

## HTTP

`POST api/Feedback/attachments` sends the file as the body, not JSON. The media type is the
file's type, with no charset. The file name is the percent-encoded `X-File-Name` header.
`HttpApiService` does this for any `IFileUploadRequest`.

`GET api/Feedback/attachments/{id}` writes the bytes. Images use `Content-Disposition: inline`.
PDF and text use `attachment`. The response sets `X-Content-Type-Options: nosniff` and
`Cache-Control: private, no-store`. The `Content-Type` is the allow-list spelling.

An unlinked file can be downloaded or removed only by the person who uploaded it. After it is
filed, the item's owner or a principal with `admin.access` on `identity-session` or
`mock-identity-session` can download it. Anyone else gets the same 404 as a missing id.
Removing a filed file is HTTP 409.

`DELETE api/Feedback/attachments/{id}` is the generated JSON endpoint. Upload and download are
hand-written endpoints because the generated one always writes JSON.

## Client

The file picker and the paste hook both call `UploadFeedbackAttachment`. Paste is a small
module on the Details box (`feedback-paste.ts`). A successful upload appends a markdown link
when it fits in the body. The item page lists the stored files and shows an image when the
type is `image/*`. The body stays plain text.
