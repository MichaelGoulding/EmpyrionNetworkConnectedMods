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




## DiscordBotMod

-img 1

1) Navigate to [https://discordapp.com/developers/applications/me](https://discordapp.com/developers/applications/me)
2) Select and create a new App

-img 2

3) Enter in the application name for your mod
4) Enter in a description
5) Select Create App

-img 3

6) After your application has been created you will see the settings screen for your application,
   scroll down and in the Bot section click on `Create a Bot User`
   
-img 4

7) You will get a warning message stating that your bot will be visible after it's created.
   Go ahead and click `Yes, do it`.
   
-img 5

8) After your bot has been created you need to aquire your token.  Click on reveal token.

-img 6

9) Copy your token to the clipboard

-img 7

10) Paste it in your DiscordBotMod_Settings.yaml file in the DiscordToken property.

-img 8

11) Back in discord go into the user settings and select the `Appearance` category.  Scroll down
    to where it says advanced and tick the `Developer Mode` option on.
    
-img 9

12) With the channel that you want the bot to be in, right click it and select `Copy ID`

-img 10

13) Paste the ID in the ChannelId property in DiscordBotMod_settings.yaml

-img 11

14) In visual studio right make sure your build configuration is set to `Release`
15) Right click on your DiscordBotMod project and select build.

-img 12

16) in the Build directory `EmpyrionNetworkConnectedMods\NetworkConnectedModRunner\bin\Release\Extensions`
    you will see a `DiscordBotMod` folder.  Copy that to your clipboard.
    
-img 13

17 in the Mod directory of Empyrion `Empyrion\Content\Mods` paste the built directory you've copied from the previous
   step