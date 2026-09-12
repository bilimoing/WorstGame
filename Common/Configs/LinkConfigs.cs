using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;

namespace WorstGame.Common.Configs;

public class LinkConfigs : ModConfig
{
    public static LinkConfigs Instance;
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [CustomModConfigItem(typeof(BigTextElement))]
    public object LinkWarningText;

    [CustomModConfigItem(typeof(ImproveGameHeaderElement))]
    public object ImproveGameHeader;

    [DefaultValue(false)] 
    [ReloadRequired] 
    public bool BigBag;
}

public class ImproveGameHeaderElement : ConfigElement
{
    private Asset<Texture2D> _iconTexture;

    public override void OnBind()
    {
        base.OnBind();
        Height.Set(30f, 0f);
        _iconTexture ??= ModContent.Request<Texture2D>("WorstGame/Assets/Textures/ImproveGame");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        float x = dimensions.X;
        float y = dimensions.Y;
        if (_iconTexture is { IsLoaded: true })
        {
            Texture2D texture = _iconTexture.Value;
            float scale = 0.8f;
            Vector2 position = new(x + 10f, y + (dimensions.Height - texture.Height * scale) / 2f);
            spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            x += texture.Width * scale + 15f;
        }
        else
        {
            x += 10f;
        }

        string text = Language.GetTextValue("Mods.WorstGame.Configs.LinkConfigs.ImproveGameHeader.Label");
        var font = FontAssets.DeathText.Value;
        float textScale = 0.4f;
        Color color = Color.White;
        Vector2 textSize = font.MeasureString(text) * textScale;
        Vector2 textPos = new(x, y + (dimensions.Height - textSize.Y) / 2f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, color, 0f, Vector2.Zero, new Vector2(textScale), -1f, 1.5f);
    }
}

public class BigTextElement : ConfigElement
{
    public override void OnBind()
    {
        base.OnBind();
        Height.Set(50f, 0f);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        float x = dimensions.X + dimensions.Width / 2f;
        float y = dimensions.Y + dimensions.Height / 2f;
        string text = "!!! 联动内容需要加载对应Mod才会生效 !!!";
        var font = FontAssets.DeathText.Value;
        Vector2 size = font.MeasureString(text);
        Vector2 origin = size * 0.5f;
        float time = Main.GlobalTimeWrappedHourly;
        float speed = 2f;
        float timer = (float)(Math.Sin(time * speed) + 1.0) / 2.0f;
        Color color = Color.Lerp(Color.Red, Color.Gold, timer);
        float scale = 0.45f + timer * 0.1f;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(x, y), color, 0f, origin, new Vector2(scale), -1f, 1.5f);
    }
}