using UnityEngine;
using System.Collections;

namespace CreativeSpore.SuperTilemapEditor
{    
    public static class ToolIcons
    {
        public enum eToolIcon
        {
            Pencil,
            Erase,
            Fill,
            FlipV,
            FlipH,
            Rot90,
            Info,
            Refresh,
        }

        private static float[][] s_icons = new float[][]
        {
            new float[] //Pencil
            {
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 1, 1, 1,
                0, 0, 0, 0, 1, 0, 1, 0,
                0, 0, 0, 1, 0, 1, 0, 0,
                0, 0, 1, 0, 1, 0, 0, 0,
                0, 1, 0, 1, 0, 0, 0, 0,
                1, 1, 1, 0, 0, 0, 0, 0,
                1, 1, 0, 0, 0, 0, 0, 0,
            },
            new float[] //Erase
            {
                0, 0, 0, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 0, 1, 0,
                0, 1, 1, 1, 0, 0, 0, 1,
                1, 1, 1, 1, 1, 0, 1, 0,
                0, 1, 1, 1, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0,
            },
            new float[] //Fill
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 1, 1, 0, 0, 0,
                0, 0, 1, 0, 1, 1, 1, 0,
                0, 1, 0, 0, 0, 1, 1, 1,
                1, 0, 0, 0, 0, 0, 1, 1,
                0, 1, 0, 0, 0, 1, 0, 1,
                0, 0, 1, 0, 1, 0, 0, 0,
                0, 0, 0, 1, 0, 0, 0, 0,
            },
            new float[] //FlipV
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                1, 1, 1, 1, 1, 1, 1, 1,
                1, 0, 0, 0, 1, 1, 1, 1,
                1, 0, 0, 1, 1, 1, 1, 1,
                1, 0, 1, 1, 0, 0, 1, 1,
                1, 0, 0, 1, 1, 1, 1, 1,
                1, 0, 0, 0, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1,
            },
            new float[] //FlipH
            {
                0, 1, 1, 1, 1, 1, 1, 1,
                0, 1, 0, 0, 0, 0, 0, 1,
                0, 1, 0, 0, 1, 0, 0, 1,
                0, 1, 0, 1, 1, 1, 0, 1,
                0, 1, 1, 1, 0, 1, 1, 1,
                0, 1, 1, 1, 0, 1, 1, 1,
                0, 1, 1, 1, 1, 1, 1, 1,
                0, 1, 1, 1, 1, 1, 1, 1,
            },
            new float[] //Rot90
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 1, 1, 1,
                0, 0, 0, 0, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
            },            
            new float[] //Info
            {
                1, 1, 1, 1, 1, 1, 1, 0,
                1, 0, 0, 0, 0, 0, 1, 0,
                1, 0, 1, 1, 1, 0, 1, 0,
                1, 0, 0, 0, 0, 0, 1, 0,
                1, 0, 1, 1, 0, 0, 1, 0,
                1, 0, 0, 0, 0, 0, 1, 0,
                1, 0, 0, 0, 0, 1, 1, 0,
                1, 1, 1, 1, 1, 1, 0, 0,
            },
            new float[] //Refresh
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 1, 1, 1, 0, 0, 0,
                0, 1, 0, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 0, 1, 0, 0,
                0, 0, 1, 0, 0, 1, 0, 0,
                0, 1, 1, 1, 1, 0, 0, 0,
                0, 0, 1, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
            },
        };
        private static Texture2D[] s_iconTexture = new Texture2D[s_icons.GetLength(0)];

        public static Texture2D GetToolTexture(eToolIcon toolIcon)
        {
            if(s_iconTexture[(int)toolIcon] == null)
            {
                Texture2D iconTexture = new Texture2D(8, 8);
                iconTexture.hideFlags = HideFlags.DontSave;
                iconTexture.wrapMode = TextureWrapMode.Clamp;
                Color[] colors = new Color[s_icons[(int)toolIcon].Length];
                for (int i = 0; i < colors.Length; ++i )
                {
                    colors[ (8 - 1 - (i / 8)) * 8 + i % 8 ] = new Color(1f, 1f, 1f, s_icons[(int)toolIcon][i]);
                }
                iconTexture.SetPixels(colors);
                iconTexture.Apply();
                s_iconTexture[(int)toolIcon] = iconTexture;
            }            
            return s_iconTexture[(int)toolIcon];
        }
    }
}