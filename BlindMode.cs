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


namespace BlindModeMode
{
    public class BlindMode : WildfrostMod
    {
        public BlindMode(string modDir)
            : base(modDir)
        {
            HarmonyInstance.PatchAll(typeof(PatchHarmony));
        }
        public static BlindMode instance;
        public static List<object> assets = new List<object>();
        public static bool preload = false;
        public override string GUID => "tgestudio.wildfrost.blindmode";

        public override string[] Depends => new string[] { };

        public override string Title => "Blind Mode";

        public override string Description =>
        "Got Blinded";
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
            Events.OnCardDataCreated += NoSee;
            Events.OnEntityDisplayUpdated += Add;
            base.Load();
        }

        private void Add(Entity entity)
        {
            switch (entity.data.cardType.name)
            {
                case "Enemy":
                case "Boss":
                case "Miniboss":
                case "BossSmall":
                    Change(entity);
                    break;
                case "Clunker":
                    if (entity.data.isEnemyClunker)
                        Change(entity);
                    break;
            }
        }
        private void Change(Entity target)
        {
            if ((bool)target.gameObject.GetComponentInChildren<Canvas>().transform.Find("=")) return;
            var mask = target.gameObject.GetComponentInChildren<Canvas>().transform.Find("Front");
            GameObject back = target.gameObject.GetComponentInChildren<Canvas>().transform.Find("Back").gameObject;
            var backClone = GameObject.Instantiate(back, target.gameObject.GetComponentInChildren<Canvas>().transform);
            backClone.name = "=";
            backClone.SetActive(true);
            backClone.GetComponent<AddressableSpriteLoader>().Destroy();

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

        private void NoSee(CardData card)
        {
            switch (card.cardType.name)
            {
                case "Enemy":
                case "Boss":
                case "Miniboss":
                case "BossSmall":
                    Flip(card);
                    break;
                case "Clunker":
                    if (card.isEnemyClunker)
                        Flip(card);
                    break;
            }
        }
        void Flip(CardData card)
        {
            card.forceTitle = "???";
        }
        public CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);

        public override void Unload()
        {
            base.Unload();
            Events.OnCardDataCreated -= NoSee;
            Events.OnEntityDisplayUpdated -= Add;
        }
    }
}
