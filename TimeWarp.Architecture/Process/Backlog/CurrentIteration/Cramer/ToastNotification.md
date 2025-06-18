Notification Infrastructure Architecture

Narrative

As the Architect of the web application,
I want to design a flexible notification infrastructure,
So that developers can selectively integrate various types of toast notifications for the end-user when appropriate.

Acceptance Criteria

Architectural Foundation

Given the potential need to inform users about different system events,

When designing the notification infrastructure,

Then it should:

Be capable of handling INotification messages using MediatR in conjunction with Blazor-State.

Integrate with FluentUI Toast for the visual representation of messages.

Allow developers the flexibility to publish a variety of notification types as needed by the application context.

Selective Notification Triggering

Given only specific user actions may warrant a notification,

When implementing the notification logic,

Then the infrastructure should:

Provide developers with the mechanisms to decide when and what type of notifications to trigger.

Ensure that notifications are sent only when they add value to the user experience.

Configurable Notification Management

Given the varying levels of urgency and information in notifications,

When a notification is published

Then the infrastructure should support:

Configurations for different auto-dismissal timers, user-dismissal capabilities, and styles.

Logic to handle multiple notifications, including their queuing and potential prioritization.

Implementation Notes

The system must be modular, allowing for straightforward updates or modifications to notification types.

Employ MediatR for loose coupling between notification dispatching and UI components.

Additional Recommendations

Throttle and Debounce Logic: To avoid overwhelming the user, include logic to throttle and debounce notifications, especially for real-time events or progress updates.

Comprehensive Testing Strategy: Devise a testing strategy that includes unit, integration, and end-to-end tests to ensure reliable notification delivery and presentation.

Considerations

Scalability: Ensure the notification infrastructure is scalable to handle a growing number of notifications as the application expands.

Minimal Performance Overhead: The notification system should introduce minimal latency and performance overhead to the user experience.


# Implementation Notes

The primary error handling mechanism in the application is the ProblemDetails Notification Handler. This handler is responsible for adding Notifications to State. The ProblemDetails Notification Handler is a global handler that listens for ProblemDetails notifications and displays the error message to the user.

- [ ] Implement ProblemDetails Notification Handler.
