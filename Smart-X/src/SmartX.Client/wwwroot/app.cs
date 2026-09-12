:root {
    --background-deep: #050b0a;
    --background: #07110f;
    --surface: rgba(13, 28, 24, 0.82);
--surface - solid: #0d1c18;
    --surface - raised: #12251f;
    --surface - soft: rgba(23, 49, 41, 0.58);

--border: rgba(151, 218, 193, 0.13);
--border - bright: rgba(88, 231, 187, 0.3);

--text - primary: #f3f8f5;
    --text - secondary: #a7bbb3;
    --text - muted: #71867e;

    --teal: #55e8b7;
    --teal - bright: #78f5ca;
    --teal - dark: #173f34;

    --amber: #ffb86b;
    --amber - bright: #ffd096;
    --danger: #ff7d75;

    --shadow - soft: 0 24px 70px rgba(0, 0, 0, 0.28);
--shadow - glow: 0 0 35px rgba(85, 232, 183, 0.12);

--radius - small: 12px;
--radius - medium: 18px;
--radius - large: 26px;

--transition - fast: 160ms ease;
--transition - standard: 260ms ease;
}

* {
    box - sizing: border - box;
}

html {
    min-height: 100 %;
background: var(--background - deep);
scroll - behavior: smooth;
}

body {
    min-height: 100vh;
margin: 0;
overflow - x: hidden;
color: var(--text - primary);
background:
radial - gradient(
    circle at 14 % 4 %,
    rgba(52, 170, 135, 0.16),
    transparent 31rem),
        radial - gradient(
            circle at 91 % 93 %,
            rgba(255, 184, 107, 0.09),
            transparent 27rem),
        linear - gradient(
            145deg,
            var(--background - deep),
            var(--background) 48 %,
#081510);
    font - family:
        "DM Sans",
        "Segoe UI",
        Arial,
        sans - serif;
line - height: 1.5;
-webkit - font - smoothing: antialiased;
}

button,
input,
select,
textarea {
    font: inherit;
}

button,
a {
    -webkit-tap-highlight-color: transparent;
}

button {
    color: inherit;
}

a {
    color: var(--teal);
}

a: hover {
color: var(--teal - bright);
}

h1,
h2,
h3,
h4,
p {
    margin-top: 0;
}

h1,
h2,
h3,
h4 {
    font-family:
        "Space Grotesk",
        "Segoe UI",
        sans - serif;
letter - spacing: -0.025em;
}

::selection {
color: #04100c;
    background: var(--teal);
}

::- webkit - scrollbar {
width: 10px;
height: 10px;
}

::- webkit - scrollbar - track {
background: var(--background - deep);
}

::- webkit - scrollbar - thumb {
border: 3px solid var(--background - deep);
    border - radius: 20px;
background: #29473e;
}

::- webkit - scrollbar - thumb:hover {
    background: #376052;
}

.surface - card {
border: 1px solid var(--border);
    border - radius: var(--radius - large);
background:
    linear - gradient(
        145deg,
        rgba(19, 42, 35, 0.88),
        rgba(10, 23, 19, 0.9));
    box - shadow: var(--shadow - soft);
    backdrop - filter: blur(20px);
}

.eyebrow {
    display: inline - flex;
align - items: center;
gap: 0.5rem;
margin - bottom: 1rem;
color: var(--teal);
font - family:
        "Space Grotesk",
        sans - serif;
font - size: 0.76rem;
font - weight: 700;
letter - spacing: 0.15em;
text - transform: uppercase;
}

.eyebrow::before {
width: 1.5rem;
height: 1px;
content: "";
background: var(--teal);
    box - shadow: 0 0 10px var(--teal);
}

.button - primary,
.button - secondary {
display: inline - flex;
    min - height: 46px;
    align - items: center;
    justify - content: center;
gap: 0.55rem;
padding: 0.75rem 1.15rem;
    border - radius: 14px;
cursor: pointer;
    font - weight: 700;
    text - decoration: none;
transition:
    transform var(--transition - fast),
        border - color var(--transition - fast),
        background var(--transition - fast),
        box - shadow var(--transition - fast);
}

.button - primary {
border: 1px solid var(--teal);
color: #06130f;
    background: linear - gradient(
        135deg,
        var(--teal - bright),
        var(--teal));
    box - shadow: 0 10px 28px rgba(85, 232, 183, 0.18);
}

.button - secondary {
border: 1px solid var(--border);
color: var(--text - primary);
background: var(--surface - soft);
}

.button - primary:hover,
.button-secondary:hover {
    color: inherit;
transform: translateY(-2px);
}

.button - primary:hover {
    color: #06130f;
    box - shadow: 0 14px 35px rgba(85, 232, 183, 0.28);
}

.button - secondary:hover {
    border-color: var(--border - bright);
background: rgba(31, 63, 53, 0.7);
}

.validation - message {
display: block;
    margin - top: 0.35rem;
color: var(--danger);
    font - size: 0.82rem;
}

.valid.modified:not([type = "checkbox"]) {
    border - color: var(--teal)!important;
}

.invalid {
    border-color: var(--danger)!important;
}

#blazor-error-ui {
    position: fixed;
z - index: 9999;
right: 1rem;
bottom: 1rem;
left: 1rem;
display: none;
max - width: 720px;
margin: 0 auto;
padding: 1rem 3rem 1rem 1rem;
border: 1px solid rgba(255, 125, 117, 0.35);
border - radius: 14px;
color: #ffe8e6;
    background: #321916;
    box - shadow: var(--shadow - soft);
}

#blazor-error-ui .reload {
    margin - left: 0.5rem;
color: var(--amber - bright);
font - weight: 700;
}

#blazor-error-ui .dismiss {
    position: absolute;
top: 0.7rem;
right: 1rem;
cursor: pointer;
font - size: 1.25rem;
}

.startup - loader {
display: flex;
    min - height: 100vh;
    align - items: center;
    justify - content: center;
gap: 1rem;
padding: 2rem;
color: var(--text - primary);
}

.startup - loader strong {
    font-family:
        "Space Grotesk",
        sans - serif;
font - size: 1.05rem;
letter - spacing: 0.18em;
}

.startup - loader p {
    margin: 0.25rem 0 0;
color: var(--text - muted);
font - size: 0.85rem;
}

.startup - loader__mark {
position: relative;
width: 54px;
height: 54px;
animation: loader - float 2.2s ease-in-out infinite;
}

.startup - loader__mark span {
    position: absolute;
inset: 0;
border: 1px solid var(--teal);
border - radius: 17px;
box - shadow: 0 0 20px rgba(85, 232, 183, 0.16);
animation: loader - pulse 1.8s ease-in-out infinite;
}

.startup - loader__mark span: nth - child(2) {
inset: 9px;
    border - color: var(--amber);
    animation - delay: 180ms;
}

.startup - loader__mark span: nth - child(3) {
inset: 18px;
    border - color: var(--teal - bright);
    animation - delay: 360ms;
}

@keyframes loader-pulse {
    0%,
    100% {
        opacity: 0.35;
transform: scale(0.9);
    }

    50 % {
opacity: 1;
transform: scale(1);
}
}

@keyframes loader-float {
    0%,
    100% {
        transform: translateY(0);
    }

    50 % {
transform: translateY(-5px);
}
}

@media(prefers - reduced - motion: reduce) {
    *,
    *::before,
    *::after {
        scroll - behavior: auto!important;
        animation - duration: 0.01ms!important;
        animation - iteration - count: 1!important;
        transition - duration: 0.01ms!important;
    }
}