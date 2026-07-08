# Project-Local Mini-Agents

This folder holds project-specific specialized agents — `.claude/agents/<name>.md` files with frontmatter declaring `description`, `model`, and the tools/MCP servers the agent is allowed to use.

This convention is installed by the Legislator; the routing system that decides when to reuse an existing agent versus create a new one is a separate design (the "master-agent" cycle) and is not part of this skill.
