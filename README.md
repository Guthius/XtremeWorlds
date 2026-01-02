![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-14.0-239120?style=flat-square&logo=c-sharp)
![License](https://img.shields.io/github/license/Treeflyx/XtremeWorlds?style=flat-square)
![GitHub contributors](https://img.shields.io/github/contributors/Treeflyx/XtremeWorlds?style=flat-square)
![GitHub downloads](https://img.shields.io/github/downloads/Treeflyx/XtremeWorlds/total?style=flat-square)

# XtremeWorlds Game Engine

A simple 2D MMORPG game engine written in C# using TCP .NET socket async networking.

Based on the Orion+ conversion and MirageBasic, making Mirage move to C# from Visual Basic. Mirage Source has been in works for over 20 years, and we're still a firm believer that no engine has ever came close.

Game assets such as character base are only permitted to use in this engine.

BASS audio requires a copyright license in order to use commercially, this is meant to demo the audio library since I prefer it as a developer over FAudio and Nvorbis. This is mostly due to MIDI support and a proper synthesizer. You can get a license to support the developers, [here](https://www.un4seen.com/).

## Overview

XtremeWorlds is a dynamic tile-based 2D MMORPG game engine designed for ease of use and
rapid development. The engine provides both client and server applications with
an intuitive GUI and built-in live editing features that enable seamless 
collaborative development. The GUI is rendered with the game render pipeline which allows easy skinning and customization, including in-game editors.

## Game Features

- Basic character creation with job selection
- Pixel movement and action combat
- Items & skills
- Event system
- Morals
- Jobs
- Projectiles

## Creation Features

The client has editors for the world maps, items, skills, animations, npcs, morals and more from the in-game admin panel.

### How do I access the editors?

Log in to the game with the client. On the server, type the command /access name 5 to promote yourself to owner. Now, go back to the client and tap Insert for each of the editor options. By default, the first character created is set to owner.

## Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/)
- Git

### Installation

#### 1. Clone the repository

   ```bash
   git clone https://github.com/Treeflyx/XtremeWorlds.git
   cd XtremeWorlds
   ```

#### 2. Set up PostgreSQL

- Install [PostgreSQL](https://www.postgresql.org/download/)
- Create a user with password: `mirage`, you can do this in the installer
- The database mirage is created by default from the server
- *Note: You can modify database credentials in the server settings JSON called appsettings.json located in the base directory of the server*

#### 3. Build the solution

   ```bash
   dotnet build
   ```

#### 4. Run the applications

- Start the server application first
- Launch the client application
- They will connect automatically using default settings

## Support & Community

- **Discord**: [Join our community](https://discord.gg/ARYaWbN6b2)
- **Issues**: Report bugs and request features through GitHub Issues
- **Updates**: Check releases for the latest improvements and features

## License

See the [LICENSE](LICENSE) file for details.
