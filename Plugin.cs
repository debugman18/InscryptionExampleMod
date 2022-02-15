using BepInEx.Logging;
using System;
using System.Reflection;
using HarmonyLib;
using BepInEx;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DiskCardGame;
using UnityEngine;
using InscryptionAPI;
using InscryptionAPI.Saves;
using InscryptionAPI.Card;
using InscryptionAPI.Ascension;
using InscryptionAPI.Helpers;
using System.Linq;

namespace ExampleMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("cyantist.inscryption.api", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // Declare Harmony here for future Harmony patches. You'll use Harmony to patch the game's code outside of the scope of the API.
        Harmony harmony = new Harmony(PluginGuid);

        // These are variables that exist everywhere in the entire class.
        private const string PluginGuid = "debugman18.inscryption.examplemod";
        private const string PluginName = "ExampleMod";
        private const string PluginVersion = "2.0";

        // For some things, like challenge icons, we need to add the art now instead of later.
        // We initialize the list here, in Awake() we'll add the sprites themselves.
        public static List<Sprite> art_sprites;

        // The first one is the actual challengeinfo, the second is the one we will check when applying the challenge effects.
        private static AscensionChallengeInfo exampleChallenge_info;
        private static AscensionChallengeInfo exampleChallenge;

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is where you would run all of your other methods.
        private void Awake()
        {
            Logger.LogInfo($"Loaded {PluginName}!");

            // Here we add the sprites to the list we created earlier.
            art_sprites = new List<Sprite>();

            Sprite example_sprite = TextureHelper.GetImageAsSprite("ascensionicon_example.png", TextureHelper.SpriteType.ChallengeIcon);
            example_sprite.name = "ascensionicon_example";
            art_sprites.Add(example_sprite);

            Sprite example_activated_sprite = TextureHelper.GetImageAsSprite("ascensionicon_activated_example.png", TextureHelper.SpriteType.ChallengeIcon);
            example_activated_sprite.name = "ascensionicon_activated_example";
            art_sprites.Add(example_activated_sprite);

            // Add abilities before cards. Otherwise, the game will try to load cards before the abilities are created.

            // The example ability method.
            AddNewTestAbility();

            // The example card method.
            AddBears();

            // The example challenge method. The method creates the challengeinfo, the second line here passes the info to the API.
            AddExampleChallenge();
            exampleChallenge = ChallengeManager.Add(PluginGuid, exampleChallenge_info);

            // Adding a starter deck is fairly simple.
            // First we create the starterdeck info.
            StarterDeckInfo exampleDeck = ScriptableObject.CreateInstance<StarterDeckInfo>();
            exampleDeck.title = "ExampleDeck";
            exampleDeck.iconSprite = TextureHelper.GetImageAsSprite("starterdeck_icon_example.png", TextureHelper.SpriteType.StarterDeckIcon);
            exampleDeck.cards = new() { CardLoader.GetCardByName("Cat"), CardLoader.GetCardByName("Cat"), CardLoader.GetCardByName("Cat") };

            // Then we pass the starterdeck info to the API.
            StarterDeckManager.Add(PluginGuid, exampleDeck);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is where you would actually apply meaningful changes when checking whether or not the example challenge is active.
        // AbilityBehaviours are also an appropriate place, but what you can do this is less extensive.

        [HarmonyPatch(typeof(Card), "ApplyAppearanceBehaviours")]
        [HarmonyPostfix]
        public static void ApplyChallenge(ref Card __instance)
        {
            if (AscensionSaveData.Data.ChallengeIsActive(exampleChallenge.challengeType))
            {
                // Do your Harmony stuff here. This example method patches the 'Card' class and the 'ApplyAppearanceBehaviours' method.
                // It does nothing on its own though.
                // For documentation of Harmony patching, see:
                // https://harmony.pardeike.net/articles/patching.html
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------


        // This is a method that adds a new challenge for Kaycee's Mod.

        private void AddExampleChallenge()
        {
            exampleChallenge_info = ScriptableObject.CreateInstance<AscensionChallengeInfo>();
            exampleChallenge_info.title = "Example Challenge";
            exampleChallenge_info.description = "This is an example challenge. This does nothing.";

            // It should be obvious, but this is the point value of the challenge.
            exampleChallenge_info.pointValue = 0;

            // This is where the list we initialized comes in handy. If you try to initialize sprites here, it will not work.
            exampleChallenge_info.iconSprite = art_sprites.Find(texture => texture.name == "ascensionicon_example");
            exampleChallenge_info.activatedSprite = art_sprites.Find(texture => texture.name == "ascensionicon_activated_example");
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------


        // This is your ability class. This defines what your ability does.
        public class NewTestAbility : AbilityBehaviour
        {
            public override Ability Ability
            {
                get
                {
                    return ability;
                }
            }

            public static Ability ability;

            // Let's say we want this ability to trigger when played onto the board.
            // First we need to override RespondsToResolveOnBoard, which tells the ability whether to trigger when played.
            // You want to return true here, if you want the ability to activate upon play.
            public override bool RespondsToResolveOnBoard()
            {
                return true;
            }

            // This is the actual meat and potatoes of the ability.
            // Here is where we tell the ability what to do and when to do it.
            public override IEnumerator OnResolveOnBoard()
            {
                yield return base.PreSuccessfulTriggerSequence();
                yield return new WaitForSeconds(0.2f);
                Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, false);
                yield return new WaitForSeconds(0.25f);

                // This checks whether you have room for items.
                if (RunState.Run.consumables.Count < RunState.Run.MaxConsumables)
                {
                    RunState.Run.consumables.Add("PiggyBank");
                    Singleton<ItemsManager>.Instance.UpdateItems(false);
                }

                // The check failed, so we'll do this instead.
                else
                {
                    base.Card.Anim.StrongNegationEffect();
                    yield return new WaitForSeconds(0.2f);
                    Singleton<ItemsManager>.Instance.ShakeConsumableSlots(0f);
                }

                yield return new WaitForSeconds(0.2f);

                // Learning an ability stops the game from explaining it again.
                yield return base.LearnAbility(0f);
                yield break;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is your special ability class. This defines what your special ability does.
        public class NewTestSpecialAbility : SpecialCardBehaviour
        {
            public SpecialTriggeredAbility SpecialAbility => specialAbility;

            public static SpecialTriggeredAbility specialAbility;

            public readonly static SpecialTriggeredAbility TestSpecialAbility = SpecialTriggeredAbilityManager.Add(PluginGuid, "Test Special Ability", typeof(NewTestSpecialAbility)).Id;

            // Let's say we want this special ability to trigger when drawn.
            // First we need to override RespondsToDrawn, which tells the ability whether to trigger when drawn.
            // You want to return true here, if you want the special ability to activate upon being drawn.
            public override bool RespondsToDrawn()
            {
                return true;
            }

            // This is the actual meat and potatoes of the ability.
            // Here is where we tell the ability what to do and when to do it.
            public override IEnumerator OnDrawn()
            {
                // This checks whether you have room for items.
                if (RunState.Run.consumables.Count < RunState.Run.MaxConsumables)
                {
                    RunState.Run.consumables.Add("SquirrelBottle");
                    Singleton<ItemsManager>.Instance.UpdateItems(false);
                }

                // The check failed, so we'll do this instead.
                else
                {
                    base.Card.Anim.StrongNegationEffect();
                    yield return new WaitForSeconds(0.2f);
                    Singleton<ItemsManager>.Instance.ShakeConsumableSlots(0f);
                }

                yield break;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------


        // This method passes the ability and the ability information to the API.
        private void AddNewTestAbility()
        {
            // This builds our ability information.
            AbilityInfo newtestability = AbilityManager.New(
                PluginGuid + ".newtestability",
                "TestAbility",
                "When a card bearing this sigil deals a killing blow, it gains 1 power and then a copy of it is created in your hand.",
                typeof(NewTestAbility),
                "examplesigil.png"
            )

            // This ability will show up in the Part 1 rulebook and can appear on cards in Part 1.
            .SetDefaultPart1Ability()

            // This specifies the icon for the ability if it exists in Part 2.
            .SetPixelAbilityIcon(TextureHelper.GetImageAsTexture("pixel_examplesigil.png"), FilterMode.Point)
            ;
            
            // Pass the ability to the API.
            NewTestAbility.ability = newtestability.ability;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------


        // This method passes the card and the card information to the API.
        private void AddBears()
        {
            // This builds our card information.
            CardInfo EightFuckingBears = CardManager.New(
                // Card internal name.
                "Eight_Bears",

                // Card display name.
                "8 fucking bears!",

                // Attack.
                32,

                // Health.
                48,

                // Descryption.
                description: "Kill this abomination, please."
            )

            // This is the cost of the card. You can use bloodCost, bonesCost, and energyCost.
            .SetCost(bloodCost: 3)

            // These are the abilities this card will have.
            // The format for custom abilities is 'CustomAbilityClass.ability'.
            // The format for vanilla abilitites is Ability.Ability'.
            .AddAbilities(NewTestAbility.ability, Ability.DoubleStrike)

            // These are the special abilities this card will have.
            // These do not show up like other abilities; They are invisible to the player.
            // The format for custom special abilities is 'CustomSpecialAbilityClass.CustomSpecialAbilityID'.
            // The format for vanilla special abilities is SpecialTriggeredAbility.Ability'.
            .AddSpecialAbilities(NewTestSpecialAbility.TestSpecialAbility, SpecialTriggeredAbility.CardsInHand)

            // CardAppearanceBehaviours are things like card backgrounds.
            // In this case, the card has a Rare background.
            .AddAppearances(CardAppearanceBehaviour.Appearance.RareCardBackground)

            // MetaCategories tell the game where this card should be available as a rewward or for other purposes.
            // In this case, CardMetaCategory.Rare tells the game to put this card in the post-boss reward event.
            .AddMetaCategories(CardMetaCategory.Rare)

            // The first image is the card portrait.
            // The second image is the emissive portrait.
            .SetPortrait("eightfuckingbears.png", "eightfuckingbears_emissive.png")

            ;

            // Pass the card to the API.
            CardManager.Add(EightFuckingBears);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

    }
}
