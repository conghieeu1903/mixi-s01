# ss-go Lua AI Contract Pack

This package helps AI agents generate and review Lua 5.1 scripts for the ss-go server.

## Package Layout

```text
.agents/
└── skills/
    └── ss-go-lua/
        ├── SKILL.md
        ├── agents/
        │   └── openai.yaml
        ├── references/
        │   ├── CONTRACT.md
        │   ├── RETURN_SCHEMAS.md
        │   └── AGENT_PROMPT.md
        └── assets/
            ├── ss-go-game-api.lua
            └── luarc.snippet.json

.saigame/
├── README.md
└── manifest.json
```

| File | Purpose |
| --- | --- |
| `SKILL.md` | Tells compatible agents when and how to apply the ss-go Lua contract. |
| `agents/openai.yaml` | Provides Codex-facing display metadata and a default invocation prompt. |
| `references/CONTRACT.md` | Canonical sandbox, globals, error handling, `game.*` API, and library-script rules. |
| `references/RETURN_SCHEMAS.md` | Canonical field-level schemas for table and list values returned by `game.*`. |
| `references/AGENT_PROMPT.md` | Standalone fallback prompt for AI tools that cannot load Agent Skills. |
| `assets/ss-go-game-api.lua` | Editor-only LuaLS annotations for globals, functions, parameters, and return types. |
| `assets/luarc.snippet.json` | Lua Language Server settings to merge into an existing project configuration. |
| `.saigame/README.md` | Human-facing installation and usage guide. |
| `.saigame/manifest.json` | Contract-pack name, version, skill identifier, and source repository name. |

## Install in a Game Project

Copy the **contents** of this package into the game-project root:

```text
<game-project>/
├── .agents/skills/ss-go-lua/
└── .saigame/
    ├── README.md
    └── manifest.json
```

Do not copy the outer pack directory. Merge `.agents` and `.saigame` with existing directories. This does not replace root `README.md`, `AGENTS.md`, `GEMINI.md`, Copilot instructions, or `.luarc.json`.

## Version

Read `version` from `.saigame/manifest.json`. Versions follow SemVer.

## Use

Compatible agents can discover the skill automatically. To activate it explicitly:

| Agent | Example request |
| --- | --- |
| Codex | `Use $ss-go-lua to generate this Lua script.` |
| GitHub Copilot | `Use the /ss-go-lua skill to generate this Lua script.` |
| Google Antigravity | `Use the ss-go-lua skill to generate this Lua script.` |

Also provide the requested logic and the expected `payload` and `output` schemas.

For AI tools without Agent Skills, paste:

```text
.agents/skills/ss-go-lua/references/AGENT_PROMPT.md
.agents/skills/ss-go-lua/references/RETURN_SCHEMAS.md
```

## Lua Language Server

Merge this snippet into the project's existing LuaLS configuration:

```text
.agents/skills/ss-go-lua/assets/luarc.snippet.json
```

Do not execute `ss-go-game-api.lua`; it is an editor-only stub.

## Source of Truth

1. `references/CONTRACT.md`: runtime rules and `game.*` APIs.
2. `references/RETURN_SCHEMAS.md`: returned fields.
3. `assets/ss-go-game-api.lua`: editor annotations.

The game team must still provide its own `payload`, `output`, metadata, stats, and battle-state schemas.
