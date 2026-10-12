# Grok conversation: feedback admin (2026-10-12)

Source: https://grok.com/share/bGVnYWN5LWNvcHk_d4f68045-abfd-476b-8fab-af33084d818e
(captured verbatim from the share API on 2026-10-12)

## Steve
Give me examples of user feedback websites, like maybe, I don't know, Zendesk, UserVoice. Our application takes feedback, but from the admin side, how do we manage it? Is there some kind of state that we've reviewed it, and how do they reply back to the original filer, etc.?
## Grok
The category you're describing is a **feedback board** — and the admin side is exactly what these tools are built around. The big names: Canny, UserVoice, Productboard, Featurebase, UserJot, and Sleekplan. Canny is the default pick for mid-market teams; UserVoice is the enterprise option at roughly a thousand dollars a month.

On the admin side, the pattern is consistent across all of them. Each piece of feedback gets a **status** — Open, Under Review, Planned, In Progress, Completed, Declined — and admins can add custom ones. You can merge duplicates, tag and categorize, and assign owners.

For replying to the original filer, there are two mechanisms. Public comments on the post, which everyone sees, and private replies that go only to the submitter — usually by email. Canny's Autopilot even generates smart replies and can pull feedback automatically from Zendesk or Intercom conversations.

The loop closes when a status changes: voters get notified automatically, so you don't have to email everyone manually when something ships.

If you're building this into your own app rather than adopting one, the core data model is just a status enum, an assignee, a comment thread, and an email notification on status change. Want me to sketch that schema?
## Steve
I am building it into the Timewarp architecture feedback system. We've got the way to take a feedback, but we don't have any way to, uh, administer the feedback that we took. So, yeah, go ahead and, actually, you might even be able to see the repo. I'm pretty sure you can 'cause we've got it connected.
## Grok
I can see the repo, and the gap is exactly where you said it is. The current design is deliberately filer-only: the FeedbackItem aggregate is immutable after File — no status, no assignment, no edit — and the store only has Add, Find, ListByOwner, and Remove. The EF config even says it out loud: "There is no cross-owner query." So an admin literally cannot list everything today.

The good news is the seams are already there. The store is a port, so you can add ListAllAsync and UpdateAsync without touching handlers. The mail stack already exists — IEmailSender, IProfileEmailLookup, the receipt email in submit-feedback — so replying to a filer is mostly a new handler reusing that path. And the SPA already has a feedback-state pattern you can mirror for an admin view.

The one real design decision is the status model. Canny-style statuses — Open, Under Review, Planned, In Progress, Completed, Declined — are a closed enum, which fits your FeedbackKind pattern nicely. But if you want free-form triage notes, that's a separate comment entity, and that's where the reply-to-filer feature lives too.

My lean: start with the status enum plus a SetStatus command, then a ListAll query gated on an admin role, then the reply handler. Want me to sketch the domain types for that first slice?
