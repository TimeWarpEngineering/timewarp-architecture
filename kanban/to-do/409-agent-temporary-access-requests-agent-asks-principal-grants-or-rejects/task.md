# Agent temporary access requests: agent asks, principal grants or rejects

## Description

An idea, not a design. Steven's words:

> We have ability to link an agent to a principal and often we would want them to have read access
> but occasionally they need more access, would be nice if they could request temp access and the
> principal could Grant or reject.

The shape of the idea:

- An agent is linked to a principal (this already exists).
- The agent's default access is read-only.
- When the agent needs more, it can request elevated access for a bounded time.
- The principal approves or rejects the request.
- Approved access ends on its own when the time is up.

Nothing below is decided. This task exists to capture the idea and the questions so a later pass
can turn it into a spec (and, if it earns it, a spike).

## Open questions

- **Scope:** what can be requested? A single resource, an action type, a whole area, or a named
  role? Who defines the levels?
- **Duration:** is the time bound chosen by the agent, by the principal, or fixed by policy? Is
  there a maximum?
- **Expiry:** what happens at expiry, and to work in flight? Can a grant be renewed or revoked
  early?
- **Request content:** what must the agent say (reason, resource, requested level, duration)?
- **Decision options:** only grant or reject, or also grant a narrower scope or shorter time than
  asked?
- **Audit trail:** what is recorded for each request, decision and use of the elevated access, and
  where is it kept and shown?
- **Notification channel:** how does the principal learn a request is waiting (in-app, email,
  chat, push), and what happens when nobody answers (timeout, default reject)?
- **Principal absent or delegated:** can a principal delegate approval, or set standing rules
  (for example, auto-approve a given scope)?
- **Abuse limits:** rate limits or repeated-request handling so an agent cannot wear the principal
  down.
- **Where it lives:** which existing architecture piece owns the agent-to-principal link today,
  and does this extend it or sit beside it?

## Related pattern (inspiration pointer only)

The intel evaluation of UniKon / Temper describes a similar pattern: denied actions become pending
decisions that a human approves at narrow, medium or broad scope. It is an inspiration pointer, not
a decision to adopt it.

- https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-10-23-arni-unikon-temper-executable-statecharts.md

## Checklist

- [ ] Review how the agent-to-principal link works today
- [ ] Answer the open questions above
- [ ] Decide whether this becomes a spec, a spike, or stays an idea
- [ ] Write the spec or spike task if it proceeds

## Session

- Created: 291090 (2026-10-23)

## Notes

Captured from Steven's chat on 2026-10-23. Owner: Architecture bot. Not launched or claimed for
implementation.
