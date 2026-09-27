# Bannerlord CampaignBehavior Skill Local Dump

Event subscription catalog, dialogue/menu injection, and anti-lag time-slicing patterns for CampaignBehaviorBase in Mount & Blade II Bannerlord.
Key patterns:
- RegisterEvents with AddNonSerializedListener
- SyncData stateless or stateful
- Avoid engine initialization crash constraints
- Anti-shadowing constraints (no Campaign folder/namespace/class)
