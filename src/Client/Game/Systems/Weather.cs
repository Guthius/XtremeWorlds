using Core.Globals;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection.Metadata;

namespace Client
{

    public class Weather
    {

        #region Functions

        public static void OnUpdate()
        {
            int i;
            int x;

            if (GameState.CurrentWeather > 0 & GameState.CurrentWeather < (int)WeatherType.Fog)
            {
                if (GameState.CurrentWeather == (int)WeatherType.Rain | GameState.CurrentWeather == (int)WeatherType.Storm)
                {
                    Audio.PlayWeatherSound("Rain.ogg", true);
                }

                x = GameLogic.Rand(1, Variables.MaxWeatherParticles - GameState.CurrentWeatherIntensity);
                if (x == 1)
                {
                    // Add a new particle
                    for (i = 0; i < Variables.MaxWeatherParticles; i++)
                    {
                        if (GameState.WeatherParticle[i].InUse == 0)
                        {
                            if (GameLogic.Rand(1, 3) == 1)
                            {
                                GameState.WeatherParticle[i].InUse = 1;
                                GameState.WeatherParticle[i].Type = GameState.CurrentWeather;
                                GameState.WeatherParticle[i].Velocity = GameLogic.Rand(8, 14);
                                GameState.WeatherParticle[i].X = (int)Math.Round(GameState.TileView.Left * 32d - 32d);
                                GameState.WeatherParticle[i].Y = (int)Math.Round(GameState.TileView.Top * 32d + GameLogic.Rand(-32));
                            }
                            else
                            {
                                GameState.WeatherParticle[i].InUse = 1;
                                GameState.WeatherParticle[i].Type = GameState.CurrentWeather;
                                GameState.WeatherParticle[i].Velocity = GameLogic.Rand(10, 15);
                                GameState.WeatherParticle[i].X = (int)Math.Round(GameState.TileView.Left * 32d + GameLogic.Rand(-32, GameState.ResolutionWidth));
                                GameState.WeatherParticle[i].Y = (int)Math.Round(GameState.TileView.Top * 32d - 32d);
                            }
                        }
                    }
                }
            }
            else
            {
                Audio.StopWeatherSound();
            }

            if (GameState.CurrentWeather == (int)WeatherType.Storm)
            {
                x = GameLogic.Rand(1, 400 - GameState.CurrentWeatherIntensity);
                if (x == 1)
                {
                    GameState.DrawThunder = GameLogic.Rand(15, 22);
                    Audio.PlayExtraSound("Thunder.ogg");
                }
            }

            for (i = 0; i < Variables.MaxWeatherParticles; i++)
            {
                if (GameState.WeatherParticle[i].InUse == 1)
                {
                    // Only fall vertically, not diagonally
                    if (GameState.WeatherParticle[i].Y > GameState.TileView.Bottom * 32d)
                    {
                        GameState.WeatherParticle[i].InUse = 0;
                    }
                    else
                    {
                        GameState.WeatherParticle[i].Y += GameState.WeatherParticle[i].Velocity;
                    }
                }
            }

        }

        public static void OnDraw()
        {
            int i;
            int spriteLeft;

            for (i = 0; i < Variables.MaxWeatherParticles; i++)
            {
                if (Conversions.ToBoolean(GameState.WeatherParticle[i].InUse))
                {
                    if (GameState.WeatherParticle[i].Type == (int)WeatherType.Storm)
                    {
                        spriteLeft = 0;
                    }
                    else
                    {
                        spriteLeft = GameState.WeatherParticle[i].Type - 1;
                    }

                    string argPath = System.IO.Path.Combine(DataPath.Misc, "Weather");
                    GameClient.RenderTexture(ref argPath, GameLogic.ConvertMapX(GameState.WeatherParticle[i].X), GameLogic.ConvertMapY(GameState.WeatherParticle[i].Y), spriteLeft * 32, 0, 32, 32, 32, 32);
                }
            }

            if (GameState.DrawThunder > 0)
            {
                // Create a temporary texture matching the camera size
                using (var thunderTexture = new Texture2D(GameClient.Graphics?.GraphicsDevice, GameState.ResolutionWidth, GameState.ResolutionHeight))
                {
                    // Create an array to store pixel data
                    var whitePixels = new Microsoft.Xna.Framework.Color[(GameState.ResolutionWidth * GameState.ResolutionHeight)];
                    var count = 0;

                    // Fill the pixel array with semi-transparent white pixels
                    for (i = 0, count = whitePixels.Length; i < count; i++)
                        whitePixels[i] = new Microsoft.Xna.Framework.Color(255, 255, 255, 150); // White with 150 alpha

                    // Set the pixel data for the texture
                    thunderTexture.SetData(whitePixels);

                    // Begin SpriteBatch to render the thunder effect
                    GameClient.SpriteBatch?.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    GameClient.SpriteBatch?.Draw(thunderTexture, new Microsoft.Xna.Framework.Rectangle(0, 0, GameState.ResolutionWidth, GameState.ResolutionHeight), Microsoft.Xna.Framework.Color.White);
                    GameClient.SpriteBatch?.End();
                }

                // Decrease the thunder counter
                GameState.DrawThunder -= 1;
            }
        }

        #endregion

    }
}