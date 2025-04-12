using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deadpan.Enums.Engine.Components.Modding;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using System;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Components;
using System.Collections;
using WildfrostHopeMod;
using UnityEngine.Localization;


namespace BlindModeMode
{
    public enum BlindingMode
    {
        Enemy,
        Friendly,
        Item,
        EnemyAndFriendly,
        EnemyAndItem,
        FriendlyAndItem,
        EnemyAndFriendlyAndItem,
    }
    public class BlindMode : WildfrostMod
    {
        public BlindMode(string modDir)
            : base(modDir)
        {
            HarmonyInstance.PatchAll(typeof(PatchHarmony));
        }
        public static BlindMode instance;
        public static List<object> assets = new List<object>();
        public List<Entity> copies = new List<Entity>();
        public static bool preload = false;
        public override string GUID => "tgestudio.wildfrost.blindmode";

        public override string[] Depends => new string[] { "hope.wildfrost.configs" };

        public override string Title => "Blind Mode";

        public override string Description =>
        "Got Blinded";

        [ConfigItem(BlindingMode.Enemy, "", "Blind Mode")]
        [ConfigManagerTitle("Blind Mode")]
        [ConfigManagerDesc("Lmao")]
        [ConfigOptions(typeof(BlindingMode))]
        public BlindingMode blindmode;
        public override List<T> AddAssets<T, Y>()
        {
            if (assets.OfType<T>().Any())
                Debug.LogWarning($"[{Title}] adding {typeof(Y).Name}s: {assets.OfType<T>().Count()}");
            return assets.OfType<T>().ToList();
        }
        public T TryGet<T>(string name) where T : DataFile
        {
            T data;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                data = base.Get<StatusEffectData>(name) as T;
            else if (typeof(KeywordData).IsAssignableFrom(typeof(T)))
                data = base.Get<KeywordData>(name.ToLower()) as T;
            else
                data = base.Get<T>(name);

            if (data == null)
                throw new Exception($"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{name}] or [{Extensions.PrefixGUID(name, this)}]");

            return data;
        }

        public override void Load()
        {
            instance = this;
            base.Load();
            Events.OnEntityDisplayUpdated += Add;
            Events.OnEntityCreated += Add;
            Events.OnBackToMainMenu += UnchangeAll;
            Events.OnEntityDestroyed += Remove;
        }

        private void Remove(Entity arg0)
        {
            if (Battle.instance == null)
                UnchangeAll();
        }

        private void UnchangeAll()
        {
            foreach (var item in copies)
            {
                Unchange(item);
            }
            copies.Clear();
        }

        private void Add(Entity entity)
        {
            if (!GetCardTypes().Contains(entity.data.cardType.name) || Battle.instance == null)
            {
                Unchange(entity);
            }
            if (Battle.instance != null)
                if (GetCardTypes().Contains(entity.data.cardType.name))
                {
                    bool isClunker = entity.data.cardType.name == "Clunker";
                    bool isSummoned = entity.data.cardType.name == "Summoned";
                    bool isEnemyClunker = entity.data.isEnemyClunker || entity.owner != Battle.instance.player;
                    bool isEnemySummoned = entity.owner != Battle.instance.player;
                    bool blindEnemy = blindmode == BlindingMode.Enemy || blindmode == BlindingMode.EnemyAndItem || blindmode == BlindingMode.EnemyAndFriendlyAndItem;

                    if (isClunker)
                    {
                        if ((blindEnemy && isEnemyClunker) || (!blindEnemy && !isEnemyClunker))
                        {
                            Change(entity);
                            return;
                        }
                    }
                    else if (isSummoned)
                    {
                        if ((blindEnemy && isEnemySummoned) || (!blindEnemy && !isEnemySummoned))
                        {
                            Change(entity);
                            return;
                        }
                    }
                    else
                    {
                        Change(entity);
                    }
                }
        }
        public string[] GetCardTypes()
        {
            switch (blindmode)
            {
                case BlindingMode.Enemy:
                    return new string[] { "Enemy", "Boss", "Miniboss", "BossSmall", "Clunker", "Summoned" };
                case BlindingMode.Friendly:
                    return new string[] { "Friendly", "Clunker", "Summoned" };
                case BlindingMode.Item:
                    return new string[] { "Item" };
                case BlindingMode.EnemyAndFriendly:
                    return new string[] { "Friendly", "Enemy", "Boss", "Miniboss", "BossSmall", "Clunker", "Summoned" };
                case BlindingMode.EnemyAndItem:
                    return new string[] { "Item", "Enemy", "Boss", "Miniboss", "BossSmall", "Clunker", "Summoned" };
                case BlindingMode.FriendlyAndItem:
                    return new string[] { "Friendly", "Item", "Clunker", "Summoned" };
                case BlindingMode.EnemyAndFriendlyAndItem:
                    return new string[] { "Friendly", "Item", "Enemy", "Boss", "Miniboss", "BossSmall", "Clunker", "Summoned" };
                default:
                    return new string[] { "Enemy", "Boss", "Miniboss", "BossSmall", "Clunker", "Summoned" };
            }
        }
        private void Change(Entity target)
        {
            if ((bool)target.GetComponentInChildren<Canvas>().transform.Find("=")) return;

            StringTable collection = LocalizationHelper.GetCollection("Cards", new LocaleIdentifier(SystemLanguage.English));
            collection.SetString("Mystery_title", "???");
            target.data.titleKey = collection.GetString("Mystery_title");

            var mask = target.gameObject.GetComponentInChildren<Canvas>().transform.Find("Front");
            GameObject back = target.gameObject.GetComponentInChildren<Canvas>().transform.Find("Back").gameObject;
            var backClone = GameObject.Instantiate(back, target.gameObject.GetComponentInChildren<Canvas>().transform);
            backClone.name = "=";
            backClone.SetActive(true);
            copies.Add(target);
            //backClone.GetComponent<AddressableSpriteLoader>().Destroy();

            foreach (var frame in mask.GetAllChildren())
            {
                if (frame.name.Contains("Mask"))
                {
                    frame.GetComponent<Image>().enabled = false;
                    foreach (Transform item in frame)
                    {
                        if (item.name != "Interaction") item.gameObject.SetActive(false);
                    }
                    continue;
                }
                frame.gameObject.SetActive(false);
            }
        }
        private void Unchange(Entity target)
        {
            var canvas = target.GetComponentInChildren<Canvas>().transform;
            var equalClone = canvas.Find("=");

            if (equalClone == null) return;

            StringTable collection = LocalizationHelper.GetCollection("Cards", new LocaleIdentifier(SystemLanguage.English));
            target.data.titleKey = collection.GetString(target.data.name + "_title");

            GameObject.Destroy(equalClone.gameObject);

            var mask = canvas.Find("Front");
            if (mask != null)
            {
                foreach (var frame in mask.GetAllChildren())
                {
                    if (frame.name.Contains("Mask"))
                    {
                        var image = frame.GetComponent<Image>();
                        if (image != null) image.enabled = true;

                        foreach (Transform item in frame)
                        {
                            item.gameObject.SetActive(true);
                        }

                        continue;
                    }

                    frame.gameObject.SetActive(true);
                }
            }
        }

        internal Sprite ScaledSprite(string fileName, int pixelsPerUnit = 100)
        {
            Texture2D tex = ImagePath(fileName).ToTex();

            // Convert 2-pixel offset to normalized Y pivot offset
            float offsetY = 110f / tex.height;

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, (20f * pixelsPerUnit) / (tex.height * 100f) + offsetY),
                pixelsPerUnit
            );
        }
        public CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);

        public override void Unload()
        {
            base.Unload();
            Events.OnEntityDisplayUpdated -= Add;
            Events.OnEntityCreated -= Add;
            Events.OnBackToMainMenu -= UnchangeAll;
            Events.OnEntityDestroyed -= Remove;
            UnchangeAll();
        }
    }
}
