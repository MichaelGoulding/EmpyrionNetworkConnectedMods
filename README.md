# Introduction
This is a wrapper for the Empyrion Mod API and a system to run mods without having to restart the game server.  It uses the existing TCP port used by EAH to connect to the game server.

# Getting Started

## Build
1.	Clone the code (recursively to include submodules).
2.	Load up in Visual Studio 2017
3.  Build all.

 ## Run
After building, the \NetworkConnectedModRunner\bin\Release (or Debug) folder will contain the .exe which will load an run any *Mod.dll found in the Extensions folder (or 1 folder deeper).
Make sure you configure the Settings.yaml file to point to the API port used by EAH.

# Contribute
Send bugs or send pull requests with any improvements, modules, or documentation.

# Included mods
| Module | Description |
|:-----------|:-----------|
| DiscordBotMod | Connects in-game general chat to a specific channel in your Discord server.  Two-way communication. |
| VotingRewardMod | Calls empyrion-servers.com REST API to give configured rewards to players every day they vote. |
| SellToServer | Lets you configure an area where you can type /sell and sell items back to the server for credits.  Prices are configured in yaml (including a default price if you want to accept any item). |
| PlayfieldStructureRegenMod| Regenerates POIs or asteriods when a playfield is loaded. |
| FactionPlayfieldKickerMod| Keeps people not belonging to a specifc faction out of a playfield if they try to warp in. |
| StructureOwnershipMod | Not done. It gives periodic rewards to any faction that captures the core of configured ships/buildings you then use /income to take out the items you've earned. the idea is that it gives purpose in PVP to take over bases in space/planet it also reduce the need to use autominers as you could capture a "steal block" factory or something |