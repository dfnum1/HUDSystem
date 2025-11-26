using DaVikingCode.RectanglePacking;
using Framework.HUD.Runtime;
using PlasticGui.WebApi.Responses;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace DynamicAtlas
{
    internal struct TextureAsset
    {
        public string name { get; private set; }
        public int index { get; private set; }
        public int x { get; private set; }
        public int y { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }
        public TextureAsset(string name, int index, int x, int y, int width, int height)
        {
            this.name = name;
            this.index = index;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    internal struct ProcessAtlas
    {
        public Texture2D texture;
        public int oriX;
        public int oriY;
        public int oriWidth;
        public int oriHeight;
        public int index;
        public IDynamicAtlasHandle handle;
    }

    internal class DynamicAtlas : IHudAtlas
    {
        private Texture2D m_Atlas;
        private Texture2D m_AtlasMappingTex;

        private RectanglePacker m_Packer;
        private List<string> m_ProcessTextureNames;
        private List<ProcessAtlas> m_ProcessTextures;

        public bool IsFull => m_IsFull;

        private Dictionary<string, DynamicTextureData> m_UsingTexture = new Dictionary<string, DynamicTextureData>(32);
        private Dictionary<string, Sprite> m_SingleTexture = new Dictionary<string, Sprite>(32);
        private List<string> m_NeedSingleTextures = new List<string>(16);
        private bool m_IsFull;

        private bool m_EditorLog = false;
        private int m_nCurrentPackedId;
        private bool m_bNeedProcessPack = false;
        public UnityEvent<string, Sprite> OnSpriteRePacked;

        private Dictionary<string, SpriteInfo> m_vNameToSpriteInfo = new Dictionary<string, SpriteInfo>();
        //--------------------------------------------------------
        public DynamicAtlas()
        {
#if UNITY_EDITOR
            m_EditorLog = false;
#endif
            m_Atlas = null;

            var texture_size = DynamicAtlasManager.ATLAS_SIZE;
            TextureFormat format = DynamicAtlasManager.AtlasFormat;

            m_Packer = new RectanglePacker(texture_size, texture_size, DynamicAtlasManager.PADDING, 4);
            m_Atlas = new Texture2D(texture_size, texture_size, format, false);
            m_Atlas.Apply();

            m_ProcessTextureNames = new List<string>();
            m_ProcessTextures = new List<ProcessAtlas>();
        }
        //--------------------------------------------------------
        public void LateUpdate()
        {
            if (m_bNeedProcessPack)
            {
                ProcessPack();
                m_bNeedProcessPack = false;
            }
        }
        //--------------------------------------------------------
        public Texture GetAtlasTexture()
        {
            return m_Atlas;
        }
        //--------------------------------------------------------
        public int GetAtlasWidth()
        {
            return Mathf.Max(m_Atlas? m_Atlas.width:0,1);
        }
        //--------------------------------------------------------
        public int GetAtlasHeight()
        {
            return Mathf.Max(m_Atlas ? m_Atlas.height : 0, 1);
        }
        //--------------------------------------------------------
        public Texture2D  GetAtlasMappingTex()
        {
            if(m_AtlasMappingTex == null)
            {
                int size = GetTexSize();
                m_AtlasMappingTex = new Texture2D(size, size, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear);
                m_AtlasMappingTex.wrapMode = TextureWrapMode.Clamp;
                m_AtlasMappingTex.filterMode = FilterMode.Point;
                m_AtlasMappingTex.SetPixels(new Color[size * size]);
                m_AtlasMappingTex.Apply();
            }
            return m_AtlasMappingTex;
        }
        //--------------------------------------------------------
        public int GetAtlasMappingWidth()
        {
            return Mathf.Max(m_AtlasMappingTex ? m_AtlasMappingTex.height : 0, 1);
        }
        //--------------------------------------------------------
        public int GetAtlasMappingHeight()
        {
            return Mathf.Max(m_AtlasMappingTex ? m_AtlasMappingTex.height : 0, 1);
        }
        //--------------------------------------------------------
        public SpriteInfo GetSpriteInfo(string name)
        {
            if (m_vNameToSpriteInfo == null)
                return SpriteInfo.DEF;
            if (m_vNameToSpriteInfo.TryGetValue(name, out var info))
            {
                return info;
            }
            if (m_UsingTexture.TryGetValue(name, out var textureData))
            {
                info = new SpriteInfo()
                {
                    name = name,
                    index = textureData.Id,
                    size = new Vector2Int(textureData.Rect.width, textureData.Rect.height),
                };
                SetSpriteUV(ref info,new Vector2(textureData.Rect.x,textureData.Rect.y), new Vector2(textureData.Rect.x+textureData.Rect.width, textureData.Rect.y + textureData.Rect.height));
                m_vNameToSpriteInfo.Add(name, info);
                m_AtlasMappingTex.Apply();
                return info;
            }
            return SpriteInfo.DEF;
        }
        //--------------------------------------------------------
        public void CheckPackSprite(Sprite sprite, bool bForceProcessPack = false)
        {
            AppendSprite(sprite, null);
            if(bForceProcessPack)
            {
                ProcessPack();
            }
        }
        //--------------------------------------------------------
        public bool AppendSprite(Sprite sprite, IDynamicAtlasHandle handle)
        {
            if (sprite == null)
                return false;
            var texture = sprite.texture;
            if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None)
            {
                Debug.LogWarning($"Cuurent Graphic Device:{SystemInfo.graphicsDeviceName}, API: {SystemInfo.graphicsDeviceType} NotSupport CopyTexture ! can not add to dynamic atlas");
                return false;
            }
            if (texture.format != m_Atlas.format)
            {
                Debug.LogWarning($"texture: {sprite.name} format is diff ,format:{texture.format}!");
                return false;
            }
            if (sprite.rect.width > DynamicAtlasManager.SINGLE_TEXTURE_MAX_SIZE || sprite.rect.height > DynamicAtlasManager.SINGLE_TEXTURE_MAX_SIZE)
            {
                Debug.LogWarning($"texture: {texture.name} size is outside {DynamicAtlasManager.SINGLE_TEXTURE_MAX_SIZE}, can not add to dynamic atlas");
                return false;
            }

            AddTextureToPack(sprite.name, sprite.texture, sprite, handle);
            m_bNeedProcessPack = true;
            return true;
        }
        //--------------------------------------------------------
        private void AddTextureToPack(string name, Texture2D texture, Sprite sprite, IDynamicAtlasHandle handle)
        {
            if (m_ProcessTextureNames.Contains(name))
                return;
            m_ProcessTextureNames.Add(name);
            ProcessAtlas process = new ProcessAtlas();
            process.texture = texture;
            if (sprite != null)
            {
                Rect spriteRect = sprite.textureRect;
                process.oriX = (int)(spriteRect.x);
                process.oriY = (int)(spriteRect.y);
                process.oriWidth = (int)spriteRect.width;
                process.oriHeight = (int)spriteRect.height;
            }
            else
            {
                process.oriX = 0;
                process.oriY = 0;
                process.oriWidth = texture.width;
                process.oriHeight = texture.height;
            }
            process.handle = handle;
            process.index = m_nCurrentPackedId++;
            m_ProcessTextures.Add(process);
        }
        //--------------------------------------------------------
        private void ProcessPack()
        {
            for (int i = 0; i < m_ProcessTextures.Count; i++)
            {
                var texture = m_ProcessTextures[i];
                m_Packer.insertRectangle(texture.oriWidth, texture.oriHeight, texture.index);
            }

            List<TextureAsset> textureAssets = UnityEngine.Pool.ListPool<TextureAsset>.Get();
            IntegerRectangle rect = new IntegerRectangle();
            int packedCount = m_Packer.packRectangles();
            for (int i = 0; i < m_ProcessTextures.Count; i++)
            {
                var process_texture_name = m_ProcessTextureNames[i];
                var process_texture = m_ProcessTextures[i];
                bool added = false;
                for (int j = 0; j < m_Packer.rectangleCount; j++)
                {
                    int id = m_Packer.getRectangleId(j);
                    if (id != process_texture.index)
                        continue;
                    rect = m_Packer.getRectangle(j, rect);
                    Graphics.CopyTexture(process_texture.texture, 0, 0, process_texture.oriX, process_texture.oriY, rect.width, rect.height,m_Atlas, 0, 0, rect.x, rect.y);
                    TextureAsset textureAsset = new TextureAsset(process_texture_name, process_texture.index, rect.x, rect.y, rect.width, rect.height);
                    textureAssets.Add(textureAsset);
                    added = true;
                    break;
                }
                if (!added)
                {
                    m_NeedSingleTextures.Add(process_texture_name);
                }
            }
            for (int i = 0; i < textureAssets.Count; i++)
            {
                var textureAsset = textureAssets[i];
                var sprite = Sprite.Create(m_Atlas, new Rect(textureAsset.x, textureAsset.y, textureAsset.width, textureAsset.height), Vector2.zero, 100, 0, SpriteMeshType.FullRect);
                if (!m_UsingTexture.TryGetValue(textureAsset.name, out var dynamicTextureData))
                {
                    dynamicTextureData = new DynamicTextureData(textureAsset.index, new IntegerRectangle(0, 0, textureAsset.width, textureAsset.height));
                    if (!m_UsingTexture.TryAdd(textureAsset.name, dynamicTextureData))
                    {
                        Debug.LogError($"UsingTexture Add Failed ! {textureAsset.name} is Added !");
                    }
                }
                dynamicTextureData.SetSprite(sprite, textureAsset.name, textureAsset.x, textureAsset.y, textureAsset.width, textureAsset.height);

                var index = m_ProcessTextureNames.IndexOf(textureAsset.name);
                if (index >=0 && index < m_ProcessTextures.Count)
                    m_ProcessTextures[index].handle?.OnDynamicAtlasDone(textureAsset.name, sprite);
            }
            if (m_EditorLog)
                Debug.Log($"DyanamicAtlas: ProcessPack Done, m_Packer.rectangleCount: {packedCount}");

            m_ProcessTextureNames.Clear();
            m_ProcessTextures.Clear();

            UnityEngine.Pool.ListPool<TextureAsset>.Release(textureAssets);
        }
        //--------------------------------------------------------
        private Sprite GetSprite(string sprite_name)
        {
            if (m_UsingTexture.TryGetValue(sprite_name, out var textureData))
            {
                textureData.AddReference();
                if (m_EditorLog)
                    Debug.Log($"DyanamicAtlas: {sprite_name} AddReference, now referecneCount: {textureData.ReferenceCount}");
                return textureData.Sprite;
            }
            if (m_SingleTexture.TryGetValue(sprite_name, out var handle))
            {
                if (m_EditorLog)
                    Debug.Log($"DyanamicAtlas: {sprite_name} Add from SingleTexture");
                return handle;
            }
            Debug.LogError($"Not Found Sprite: {sprite_name} from DynamicAtlas UsingTextures!");
            return null;
        }
        //--------------------------------------------------------
        public async Task<Sprite> GetSpriteAsync(string spritePath, CancellationToken token)
        {
            spritePath = Path.GetFileNameWithoutExtension(spritePath);
            if (m_EditorLog)
                Debug.Log($"DyanamicAtlas: Get Sprite: {spritePath}");

            if (m_ProcessTextureNames.Contains(spritePath))
            {
                while (m_bNeedProcessPack)
                {
                    await Task.Yield();
                }
            }
            if (m_UsingTexture.TryGetValue(spritePath, out var textureData))
            {
                return GetSprite(spritePath);
            }
            else
            {
                var sprite = await LoadAssetAsync(spritePath);
                if (token.IsCancellationRequested) return null;
                if (sprite != null)
                {
                    var success = AppendSprite(sprite, null);
                    if (!success)
                    {
                        m_NeedSingleTextures.Add(spritePath);
                    }
                    while (m_bNeedProcessPack)
                    {
                        await Task.Yield();
                        if (token.IsCancellationRequested) return null;
                    }
                    if (m_NeedSingleTextures.Contains(spritePath))
                    {
                        if (m_SingleTexture.ContainsKey(spritePath))
                        {
                            m_SingleTexture.Remove(spritePath);
                        }
                        m_SingleTexture.TryAdd(spritePath, sprite);
                        m_NeedSingleTextures.Remove(spritePath);
                    }
                    else
                    {
                        DynamicAtlasManager.AppendAtlasDone(spritePath, DynamicAtlasManager.eLoadResult.Success);
                    }
                    if (token.IsCancellationRequested) return null;
                    return GetSprite(spritePath);
                }
            }
            DynamicAtlasManager.AppendAtlasDone(spritePath, DynamicAtlasManager.eLoadResult.Failure);
            return null;
        }
        //--------------------------------------------------------
        public async Task<Sprite> GetSpriteAsync(Sprite sprite, CancellationToken token)
        {
           if(sprite == null)
                return null;
            if (m_EditorLog)
                Debug.Log($"DyanamicAtlas: Get Sprite: {sprite.name}");

            if (m_ProcessTextureNames.Contains(sprite.name))
            {
                while (m_bNeedProcessPack)
                {
                    await Task.Yield();
                }
            }
            if (m_UsingTexture.TryGetValue(sprite.name, out var textureData))
            {
                return GetSprite(sprite.name);
            }
            else
            {
                if (token.IsCancellationRequested) return null;
                if (sprite != null)
                {
                    var success = AppendSprite(sprite, null);
                    if (!success)
                    {
                        m_NeedSingleTextures.Add(sprite.name);
                    }
                    while (m_bNeedProcessPack)
                    {
                        await Task.Yield();
                        if (token.IsCancellationRequested) return null;
                    }
                    if (m_NeedSingleTextures.Contains(sprite.name))
                    {
                        if (m_SingleTexture.ContainsKey(sprite.name))
                        {
                            m_SingleTexture.Remove(sprite.name);
                        }
                        m_SingleTexture.TryAdd(sprite.name, sprite);
                        m_NeedSingleTextures.Remove(sprite.name);
                    }
                    else
                    {
                        DynamicAtlasManager.AppendAtlasDone(sprite.name, DynamicAtlasManager.eLoadResult.Success);
                    }
                    if (token.IsCancellationRequested) return null;
                    return GetSprite(sprite.name);
                }
            }
            DynamicAtlasManager.AppendAtlasDone(sprite.name, DynamicAtlasManager.eLoadResult.Failure);
            return null;
        }
        //--------------------------------------------------------
        public void RemoveSprite(string sprite_name)
        {
            if (m_SingleTexture.ContainsKey(sprite_name))
            {
                m_SingleTexture.Remove(sprite_name);
                if (m_EditorLog)
                    Debug.Log($"DyanamicAtlas: {sprite_name} Remove from SingleTexture");
            }
            if (m_UsingTexture.ContainsKey(sprite_name))
            {
                var textureData = m_UsingTexture[sprite_name];
                textureData.RemoveReference();
                if (m_EditorLog)
                    Debug.Log($"DyanamicAtlas: {sprite_name} RemoveReference, now referecneCount: {textureData.ReferenceCount}");
                if (textureData.ReferenceCount == 0)
                {
                    bool success = m_Packer.releaseRectangle(textureData.Id);
                    if (m_EditorLog)
                        Debug.Log($"DyanamicAtlas: Release {sprite_name} m_Packer.rectangleCount: {m_Packer.rectangleCount}");
                    if (!success)
                    {
                        Debug.LogError($"Release {sprite_name} from atlas Failed");
                        return;
                    }
                    m_UsingTexture.Remove(sprite_name);
                }
            }
        }
        //--------------------------------------------------------
        private async Task<Sprite> LoadAssetAsync(string assetName)
        {
            return await DynamicAtlasManager.LoadSpriteFunc(assetName);
        }
        //--------------------------------------------------------
        public void Init()
        {
        }
        //--------------------------------------------------------
        private int GetTexSize()
        {
            int size = 1;
            if (m_UsingTexture == null)
                return 1;
            int spriteCount = m_UsingTexture.Count;
            for (int i = 0; i <= 10; i++)
            {
                size = (int)Mathf.Pow(2, i);
                if (size * size >= spriteCount * 2)
                {
                    return size;
                }
            }
            return Mathf.Max(64, size);
        }
        //--------------------------------------------------------
        private void SetSpriteUV(ref SpriteInfo spriteInfo, Vector2 min, Vector2 max)
        {
            int atlasWidth = m_Atlas.width;
            int atlasHeight = m_Atlas.height;
            int mappingWidth = m_AtlasMappingTex.width;
            ushort spriteWidth = (ushort)((max.x - min.x) * atlasWidth);
            ushort spriteHeight = (ushort)((max.y - min.y) * atlasHeight);
            ushort spriteX = (ushort)(min.x * atlasWidth);
            ushort spriteY = (ushort)(min.y * atlasHeight);
            spriteInfo.size = new Vector2Int(spriteWidth, spriteHeight);
            byte posx0bytes = (byte)(spriteX % 256);
            byte posx1bytes = (byte)(spriteX / 256);
            byte posy0bytes = (byte)(spriteY % 256);
            byte posy1bytes = (byte)(spriteY / 256);

            byte spritew0bytes = (byte)(spriteWidth % 256);
            byte spritew1bytes = (byte)(spriteWidth / 256);
            byte spriteh0bytes = (byte)(spriteHeight % 256);
            byte spriteh1bytes = (byte)(spriteHeight / 256);

            int firstindex = spriteInfo.index * 2;
            int secondindex = firstindex + 1;

            int firstX = firstindex % mappingWidth;
            int firstY = firstindex / mappingWidth;

            int secondX = secondindex % mappingWidth;
            int secondY = secondindex / mappingWidth;

            m_AtlasMappingTex.SetPixel(firstX, firstY, new Color32(posx1bytes, posx0bytes, posy1bytes, posy0bytes));
            m_AtlasMappingTex.SetPixel(secondX, secondY, new Color32(spritew1bytes, spritew0bytes, spriteh1bytes, spriteh0bytes));
        }
        //--------------------------------------------------------
        public void GenAtlasMappingInfo(bool bForce = false)
        {
            GetAtlasMappingTex();
        }
        //--------------------------------------------------------
        internal void Destroy()
        {
            if (m_Atlas != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(m_Atlas);
                else
                    UnityEngine.Object.DestroyImmediate(m_Atlas);
                m_Atlas = null;
            }
            if(m_AtlasMappingTex!=null)
            {
                if(Application.isPlaying)
                    UnityEngine.Object.Destroy(m_AtlasMappingTex);
                else
                    UnityEngine.Object.DestroyImmediate(m_AtlasMappingTex);
                m_AtlasMappingTex = null;
            }
        }
    }
}
