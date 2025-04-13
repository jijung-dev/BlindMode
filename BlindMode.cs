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
        EnemyAndFriendly,
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

        [ConfigManagerTitle("Blinding Mode")]
        [ConfigItem(BlindingMode.Enemy, "", "Blinding Mode")]
        [ConfigOptions(typeof(BlindingMode))]
        public BlindingMode blindmode;

        [ConfigManagerTitle("Custom Boss Back")]
        [ConfigItem(true, "", "Custom Boss Back")]
        [ConfigOptions]
        public bool customBack;

        public bool blindEnemy => blindmode == BlindingMode.Enemy || blindmode == BlindingMode.EnemyAndFriendly;
        public bool blindFriendly => blindmode == BlindingMode.Friendly || blindmode == BlindingMode.EnemyAndFriendly;
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
            Events.OnEntityDisplayUpdated += FlipAll;
            Events.OnEntityCreated += FlipAll;
            Events.OnBackToMainMenu += UnFlipAll;
            Events.OnEntityDestroyed += UnFlipAll;
        }

        private void UnFlipAll(Entity arg0)
        {
            if (Battle.instance == null)
                UnFlipAll();
        }

        private void UnFlipAll()
        {
            foreach (var item in copies)
            {
                UnFlip(item);
            }
            copies.Clear();
        }

        private void FlipAll(Entity entity)
        {
            if (Battle.instance != null)
            {
                if (entity.owner != Battle.instance.player)
                {
                    if (blindEnemy) Flip(entity);
                }
                else
                {
                    if (blindFriendly) Flip(entity);
                }
            }
            else
            {
                UnFlipAll();
            }
        }
        private void Flip(Entity target)
        {
            if ((bool)target.GetComponentInChildren<Canvas>().transform.Find("="))
            {
                CheckBack(target, target.GetComponentInChildren<Canvas>().transform.Find("=").gameObject);
                return;
            }
            if (target.data.cardType.name == "Leader") return;

            StringTable collection = LocalizationHelper.GetCollection("Cards", new LocaleIdentifier(SystemLanguage.English));
            collection.SetString("Mystery_title", "???");
            target.data.titleKey = collection.GetString("Mystery_title");

            var mask = target.gameObject.GetComponentInChildren<Canvas>().transform.Find("Front");
            GameObject back = target.gameObject.GetComponentInChildren<Canvas>().transform.Find("Back").gameObject;
            var backClone = GameObject.Instantiate(back, target.gameObject.GetComponentInChildren<Canvas>().transform);
            backClone.name = "=";
            backClone.SetActive(true);
            backClone.GetComponent<AddressableSpriteLoader>().Destroy();
            CheckBack(target, backClone);

            copies.Add(target);

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
        private void UnFlip(Entity target)
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
        public void CheckBack(Entity target, GameObject backClone)
        {
            GameObject back = target.gameObject.GetComponentInChildren<Canvas>().transform.Find("Back").gameObject;
            if (customBack)
            {
                if (target.data.cardType.name == "BossSmall" || target.data.cardType.name == "Miniboss")
                {
                    backClone.GetComponent<Image>().sprite = ScaledSprite("BossSmall.png", 200, 0);
                    backClone.GetComponent<RectTransform>().sizeDelta = new Vector2(0.2f, 0f);
                }

                if (target.data.cardType.name == "Boss")
                {
                    backClone.GetComponent<Image>().sprite = ScaledSprite("Boss.png", 200, 0);
                    backClone.GetComponent<RectTransform>().sizeDelta = new Vector2(0.2f, 0f);
                }
            }
            else
            {
                if (target.data.cardType.name == "BossSmall" || target.data.cardType.name == "Miniboss")
                {
                    backClone.GetComponent<Image>().sprite = back.GetComponent<Image>().sprite;
                    backClone.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 0f);
                }

                if (target.data.cardType.name == "Boss")
                {
                    backClone.GetComponent<Image>().sprite = back.GetComponent<Image>().sprite;
                    backClone.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 0f);
                }
            }
        }

        internal Sprite ScaledSprite(string fileName, int pixelsPerUnit = 100, float offset = 110f)
        {
            Texture2D tex = ImagePath(fileName).ToTex();

            // Convert 2-pixel offset to normalized Y pivot offset
            float offsetY = offset / tex.height;

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
            Events.OnEntityDisplayUpdated -= FlipAll;
            Events.OnEntityCreated -= FlipAll;
            Events.OnBackToMainMenu -= UnFlipAll;
            Events.OnEntityDestroyed -= UnFlipAll;
            UnFlipAll();
        }
    }
}
