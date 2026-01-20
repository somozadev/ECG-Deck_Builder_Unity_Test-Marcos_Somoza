# ECG Deck Builder
### Technical Overview & Design Rationale

This project is a small vertical slice of a deckbuilder-style game built in Unity. The goal is not to present a production-ready architecture, but to demonstrate a clear 
and functional approach to UI-heavy gameplay, basic state management, and remote persistence using an external API. The scope is intentionally limited, and many decisions are 
driven by pragmatism rather than long-term robustness.

---

## Architectural Overview

The codebase is organized into a few conceptual layers that separate responsibilities without introducing heavy abstractions. Core scripts handle game flow and orchestration, 
Presentation scripts are responsible for UI and animations, Infrastructure contains external service communication such as HTTP persistence, and Services group small shared 
utilities like scene loading and user identification. This structure keeps responsibilities clear while remaining easy to read and modify.

The project deliberately avoids advanced architectural patterns. There is no dependency injection framework, no event bus, and no attempt to generalize systems beyond what is 
strictly necessary. This keeps the code approachable and easy to debug, which is often a better trade-off for prototypes or small projects.

---

## Core Gameplay Logic

The deck builder flow is driven by an explicit state machine that controls when the player can draw cards, focus them, and finalize a deck. This prevents common issues caused
by fast input and overlapping animations, such as drawing multiple cards at once or breaking the UI state. Using a simple enum-based state machine keeps the logic easy to follow 
and reason about.

The deck viewer focuses mainly on UI stability. When a card inside a layout-driven scroll view is focused, the original card is hidden using a `CanvasGroup` and a cloned “ghost”
card is animated independently. This avoids layout reflows and visual glitches that would otherwise occur if the original object were reparented or disabled. While slightly more
complex, this approach produces much more predictable UI behavior.

---

## Presentation Layer

UI components such as `CardView` are intentionally presentation-only. They handle visual state, animations, and input events, but contain no gameplay or persistence logic. 
This makes them reusable across different contexts and reduces coupling between systems.

Animations are handled with DOTween to keep the code readable and to make iteration on timing and easing straightforward. Animation logic is kept close to the UI components 
to avoid spreading visual concerns into gameplay code.

---

## Persistence and API Usage

Remote persistence is implemented using JsonBin as a lightweight stand-in backend. This allows the project to exercise real HTTP requests, async flows, and serialization without
the overhead of maintaining a custom server. All users share a single bin and are differentiated by a locally generated UUID stored in `PlayerPrefs`.

A dedicated persistence layer wraps the raw HTTP client to isolate JsonBin-specific behavior and Unity `JsonUtility` limitations. The data model is intentionally simple and 
normalized to avoid known serialization issues, prioritizing reliability over flexibility.

It is important to note that **API keys are currently exposed via the Unity Inspector**, which is not secure and would be unacceptable in a real production environment. 
This is a conscious trade-off for the purposes of this test, keeping setup simple and transparent. In a real project, these keys should be protected using a backend proxy, 
server-side authentication, or platform-specific secure storage, and never shipped directly in a client build.

---

## User Identity and Scene Flow

User identity is handled through a locally generated UUID stored in `PlayerPrefs`. This acts as a lightweight persistence key and replaces any real authentication flow. 
The intent is to validate data separation and persistence logic, not to solve user management.

Scene loading is centralized through a small utility that defines scene name constants and provides simple load helpers. This avoids duplicated strings and makes refactoring 
safer, while keeping scene transitions straightforward.

---

## Scalability Considerations

If this project were to grow, the first major improvements would involve introducing a proper domain layer decoupled from the UI, replacing JsonBin with a real backend and 
authentication flow, and improving async handling with cancellation and better error management. For larger datasets, UI virtualization and pooling would also become necessary,
along with more flexible input handling for different platforms.

---

## Final Notes

This project prioritizes clarity, determinism, and ease of iteration over long-term architectural rigor. The code is intentionally explicit and easy to follow, making it 
suitable as a starting point for further development. Many shortcuts are taken knowingly, but they are contained and easy to address if the 
project were to evolve further.
