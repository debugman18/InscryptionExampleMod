using BepInEx.Logging;
using System;
using System.Reflection;
using HarmonyLib;
using BepInEx;
using BepInEx.Bootstrap;
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
using InscryptionAPI.Encounters;
using InscryptionAPI.Regions;
using InscryptionAPI.Boons;
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
        private const string PluginPrefix = "Example";

        // For some things, like challenge icons, we need to add the art now instead of later.
        // We initialize the list here, in Awake() we'll add the sprites themselves.
        public static List<Sprite> art_sprites;

        // The first one is the actual challengeinfo, the second is the one we will check when applying the challenge effects.
        private static AscensionChallengeInfo exampleChallenge_info;
        private static AscensionChallengeInfo exampleChallenge;

        // This is the ID of our example stat icon.
        public static SpecialStatIcon ExampleStatIconID;

        // We will use this as a random seed.
        public static int randomSeed = SaveManager.SaveFile.GetCurrentRandomSeed();

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

            // In this method we're adding custom stat icons.
            AddSpecialStatIcons();

            // The example card method.
            AddBears();

            // The example challenge method. The method creates the challengeinfo, the second line here passes the info to the API.
            AddExampleChallenge();
            exampleChallenge = ChallengeManager.Add(PluginGuid, exampleChallenge_info);

            // Adding a starter deck is fairly simple.
            // First we create the starterdeck info.
            // If you're adding more than one, a method can be created to organize them.
            StarterDeckInfo exampleDeck = ScriptableObject.CreateInstance<StarterDeckInfo>();
            exampleDeck.title = "ExampleDeck";
            exampleDeck.iconSprite = TextureHelper.GetImageAsSprite("starterdeck_icon_example.png", TextureHelper.SpriteType.StarterDeckIcon);
            exampleDeck.cards = new() { CardLoader.GetCardByName("Cat"), CardLoader.GetCardByName("Cat"), CardLoader.GetCardByName("Cat") };

            // In this method we will add any custom node sequencers.
            AddNodes();

            // This adds our custom battle, known as an "encounter".
            AddExampleEncounter();

            // Example boon is added here.
            AddExampleBoon();

            // Then we pass the starterdeck info to the API.
            StarterDeckManager.Add(PluginGuid, exampleDeck);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is our stat behaviour.
        public class ExampleStatBehaviour : VariableStatBehaviour
        {
            private static SpecialStatIcon exampleStatIconType;

            public override SpecialStatIcon IconType => exampleStatIconType;

            // The array we're returning here is for the card health/power values. 
            public override int[] GetStatValues()
            {
                int num = 1;

                int[] array = new int[2];
                array[1] = num;
                return array;
            }

        }

        // Here we add a custom specialstaticon.
        public void AddSpecialStatIcons()
        {
            StatIconInfo exampleStatIconInfo = StatIconManager.New(PluginGuid, "ExampleStatIcon", "This is an example stat icon.", typeof(ExampleStatBehaviour))
                .SetIcon("ExampleMod/art/specialstaticons/example_stat_icon.png")
                .SetDefaultPart1Ability();
            exampleStatIconInfo.appliesToAttack = true;
            exampleStatIconInfo.appliesToHealth = false;
           
            ExampleStatIconID = StatIconManager.Add(PluginGuid, exampleStatIconInfo, typeof(ExampleStatBehaviour)).Id;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // Here is where we can add our nodes.
        private static void AddNodes()
        {
            // Here we add our ExampleSequence, with CustomNodeData ExampleNodeData.
            NodeManager.Add<ExampleSequence, ExampleNodeData>(
            // These textures are our node icon.
            // A tip for this art:
            // Over the four frames, mildly adjust the shape of the background object,
            // while seperately mildly adjusting the shape of the foreground object.
            // The desired animation is a mild shifting.
            new Texture2D[] {
                TextureHelper.GetImageAsTexture("example_node_1.png"),
                TextureHelper.GetImageAsTexture("example_node_2.png"),
                TextureHelper.GetImageAsTexture("example_node_3.png"),
                TextureHelper.GetImageAsTexture("example_node_4.png")
            },

            // SpecialEventRandom will spawn in areas where nodes like Totem nodes would.
            NodeManager.NodePosition.SpecialEventRandom
            );
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This creates a CardBlueprint to be used in the encounter.
        public static EncounterBlueprintData.CardBlueprint bp_Raven = new EncounterBlueprintData.CardBlueprint
        {
            // This is the card that will be played.
            card = CardLoader.GetCardByName("Raven"),

            // This is the lowest difficulty this encounter will appear for.
            minDifficulty = 0,

            // This is the highest difficulty this encounter will appear for.
            maxDifficulty = 20
        };

        // Blueprints can be chance based, as well.
        public static EncounterBlueprintData.CardBlueprint bp_SquirrelAltRaven = new EncounterBlueprintData.CardBlueprint
        {
            card = CardLoader.GetCardByName("Squirrel"),

            // We can specify an alternative card.
            replacement = CardLoader.GetCardByName("Raven"),

            // The odds are out of 100, so 50 is 50%.
            randomReplaceChance = 50
        };

        // You can also use a method with parameters.
        // You can also use parameters like replacement and randomReplaceChance the same way.
        public static EncounterBlueprintData.CardBlueprint bp_Create(string name, int minDifficulty, int maxDifficulty)
        {
            return new()
            {
                card = CardLoader.GetCardByName(name),
                minDifficulty = minDifficulty,
                maxDifficulty = maxDifficulty,
            };
        }

        // Here we declare and add our encounter.
        private static void AddExampleEncounter()
        {

            // Create the Encounter blueprint data.
            var example_blueprint = ScriptableObject.CreateInstance<EncounterBlueprintData>();
            example_blueprint.name = "ExampleBattle";
            example_blueprint.turns = new List<List<EncounterBlueprintData.CardBlueprint>>
            {
                // On the first turn, play 1 Squirrel.
                // Where these cards are played is determined by the AI.
                new List<EncounterBlueprintData.CardBlueprint> {bp_Create("Squirrel", 0, 20)},

                // On the second turn, play nothing.
                new List<EncounterBlueprintData.CardBlueprint> (),

                // On the third, and subsequent turns, play 1 Raven, increasing by 1 each turn.
                new List<EncounterBlueprintData.CardBlueprint> {bp_Raven},
                new List<EncounterBlueprintData.CardBlueprint> {bp_Raven, bp_Raven},
                new List<EncounterBlueprintData.CardBlueprint> {bp_Raven, bp_Raven, bp_Raven},
                new List<EncounterBlueprintData.CardBlueprint> {bp_Raven, bp_Raven, bp_Raven, bp_Raven},

                // On the last turn, play either a Squirrel or a Raven.
                new List<EncounterBlueprintData.CardBlueprint> {bp_SquirrelAltRaven},
            };

            // RegionIndex is the parameter of regions[]. 0 is Woodlands, 1 is Wetlands, 2 is Snowline.

            // It's helpful to clear existing encounters to test yours.
            // RegionProgression.Instance.regions[0].encounters.Clear();

            // Here we add our custom encounter to the Woodlands region.
            RegionProgression.Instance.regions[0].AddEncounters(example_blueprint);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is where we build the behaviour of the boon.
        public class ExampleBoonBehaviour : BoonBehaviour
        {
            BoonManager.FullBoon ExampleBoon = new BoonManager.FullBoon();

            public override bool RespondToSacrifice()
            {
                return true;
            }

            public override IEnumerator OnSacrifice()
            {
                RunState.Run.currency++;
                yield break;
            }
        }

        // Here is where an example boon is added.
        private static void AddExampleBoon()
        {
            // Build the boon as BoonData.
            BoonData.Type example_boon = BoonManager.New<ExampleBoonBehaviour>(
                Plugin.PluginGuid, 
                "Example Boon",
                "This example boon gives one tooth whenever a card is sacrificed.",
                TextureHelper.GetImageAsTexture("boonicon_example.png"),
                TextureHelper.GetImageAsTexture("boon_example.png"),
                true,
                true,
                true
            );

            // Pass the BoonData to the API.
            BoonManager.AddBoon(example_boon);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // Create an appearance behaviour. This can modify the card background, decals, etc.
        public class ExampleAppearanceBehaviour : CardAppearanceBehaviour
        {
            public override void ApplyAppearance()
            {
                // The lines commented out below are included as an example of changing the card background.
                // Since we will alredy be applying the Rare appearance to the example card, this part of the 
                // card appearance is commented out in order to display it. So, we'll be exclusively adding decals.

                // Texture2D ExampleBG = TextureHelper.GetImageAsTexture("card_exampleappearance.png");
                // ExampleBG.filterMode = FilterMode.Point;
                // base.Card.RenderInfo.baseTextureOverride = ExampleBG;

                // This is the decals part of a CardAppearanceBehaviour.
                Texture2D ExampleDecal = TextureHelper.GetImageAsTexture("decal_example.png");
                base.Card.Info.TempDecals.Clear();
                base.Card.Info.TempDecals.Add(ExampleDecal);
            }

            // When this card is chosen via a sequencer, run the following method. This example does nothing, but think *Ijaraq*.
            public void OnCardAddedToDeck()
            {

            }

        }

        // Pass the appearance behaviour to the API by its Id.
        public readonly static CardAppearanceBehaviour.Appearance ExampleAppearance = CardAppearanceBehaviourManager.Add(PluginGuid, "ExampleAppearance", typeof(ExampleAppearanceBehaviour)).Id;

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is where you would actually apply meaningful changes when checking whether or not the example challenge is active.
        // AbilityBehaviours are also an appropriate place, but what you can do there is less extensive.

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
                ChallengeActivationUI.Instance.ShowActivation(exampleChallenge.challengeType);
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
                "This is a test ability.",
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

                // Card ID Prefix
                modPrefix: PluginPrefix,

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
            // We also add an example appearance, which in this case only adds decals.
            .AddAppearances(CardAppearanceBehaviour.Appearance.RareCardBackground, ExampleAppearance)

            // MetaCategories tell the game where this card should be available as a rewward or for other purposes.
            // In this case, CardMetaCategory.Rare tells the game to put this card in the post-boss reward event.
            .AddMetaCategories(CardMetaCategory.Rare)

            // The first image is the card portrait.
            // The second image is the emissive portrait.
            .SetPortrait("eightfuckingbears.png")
            .SetEmissivePortrait("eightfuckingbears_emissive.png")

            ;

            EightFuckingBears.specialStatIcon = ExampleStatIconID;

            // Pass the card to the API.
            CardManager.Add(PluginPrefix, EightFuckingBears);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

    }
}
