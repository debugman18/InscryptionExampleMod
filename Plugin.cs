using BepInEx;
using BepInEx.Configuration;
using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Ascension;
using InscryptionAPI.Boons;
using InscryptionAPI.Card;
using InscryptionAPI.Helpers;
using InscryptionAPI.Nodes;
using InscryptionAPI.Regions;
using InscryptionAPI.Sound;
using InscryptionAPI.Triggers;
using InscryptionCommunityPatch.Card;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        private const string PluginVersion = "2.4.0";
        private const string PluginPrefix = "Example";

        // For some things, like challenge icons, we need to add the art now instead of later.
        // We initialize the list here, in Awake() we'll add the sprites themselves.
        public static List<Sprite> art_sprites;

        // The first one is the actual challengeinfo, the second is the one we will check when applying the challenge effects.
        private static AscensionChallengeInfo exampleChallenge_info;
        private static AscensionChallengeInfo exampleChallenge;

        // This is the ID of our example stat icon.
        public static SpecialStatIcon ExampleStatIconID;

        // We use this to reference our example tribe.
        public static Tribe exampleTribe;

        public static NewNodeManager.FullNode exampleNode;

        // We will use this as a random seed.
        public static int randomSeed;

        // Load The Config
        public ConfigEntry<bool> configEnableNothing;

        // This is where you would run all of your other methods.
        private void Awake()
        {
            Logger.LogInfo($"Loaded {PluginName}!");

            // Apply our harmony patches.
            harmony.PatchAll(typeof(Plugin));

            // Here we add the sprites to the list we created earlier.
            art_sprites = new List<Sprite>();

            Sprite example_sprite = TextureHelper.GetImageAsSprite("ascensionicon_example.png", TextureHelper.SpriteType.ChallengeIcon);
            example_sprite.name = "ascensionicon_example";
            art_sprites.Add(example_sprite);

            Sprite example_activated_sprite = TextureHelper.GetImageAsSprite("ascensionicon_activated_example.png", TextureHelper.SpriteType.ChallengeIcon);
            example_activated_sprite.name = "ascensionicon_activated_example";
            art_sprites.Add(example_activated_sprite);

            // Add abilities before cards. Otherwise, the game will try to load cards before the abilities are created.

            // Add custom cost.
            AddCustomCosts();

            // The example ability method.
            AddNewTestAbility();

            // Add custom tribe in this method.
            AddExampleTribe();

            // The example card method.
            //   AddBears();
            // AddSpecialStatIcons();
            // The example challenge method. The method creates the challengeinfo, the second line here passes the info to the API.
            // The third and fourth parameters here are Unlock Level and Stackable. 
            AddExampleChallenge();
            exampleChallenge = ChallengeManager.AddSpecific(PluginGuid, exampleChallenge_info, 0);

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

            // Pass the starterdeck info to the API.
            StarterDeckManager.Add(PluginGuid, exampleDeck);

            // Summons The Config file and or Option.
            configEnableNothing = Config.Bind<bool>("DebugMan.ExampleMod.Configs",
                                            "This Config Does Nothing?",
                                            true,
                                            "Enable NULL?");

            // --------------------------------------------------------------------------------------------------------------------------------------------------
            //Music Num
            int MusicAmount = 0;
            //Summons the music
            if (configEnableNothing.Value) // If configs value if true:
            {
                // Add Track for With sound volume lowered and with MP3
                GramophoneManager.AddTrack(PluginGuid, "Example.mp3", 0.5f);
                // Upkeep the Music Amount
                MusicAmount++;
            }
            if (configEnableNothing.Value) // If configs value if true:
            {
                // Add Track for With sound volume lowered and with MP3
                GramophoneManager.AddTrack(PluginGuid, "Example.wav");
                // Upkeep the Music Amount
                MusicAmount++;
            }
            Logger.LogInfo($"Sucsessfully Loaded {MusicAmount} Song(s)");
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // Here is where we can add our nodes.
        private static void AddNodes()
        {
            // Here we add our ExampleSequencer.
            exampleNode = NewNodeManager.New(

                guid: PluginGuid,
                name: "ExampleNode",

                // SpecialEvent will spawn in areas where nodes like Totem nodes would.
                generationType: GenerationType.SpecialEvent,

                nodeSequencerType: typeof(ExampleSequencer),

                // These textures are our node icon.
                // A tip for this art:
                // Over the four frames, mildly adjust the shape of the background object,
                // while seperately mildly adjusting the shape of the foreground object.
                // The desired animation is a mild shifting.
                nodeAnimation: new List<Texture2D>
                {
                    TextureHelper.GetImageAsTexture("example_node_1.png"),
                    TextureHelper.GetImageAsTexture("example_node_2.png"),
                    TextureHelper.GetImageAsTexture("example_node_3.png"),
                    TextureHelper.GetImageAsTexture("example_node_4.png")
                }

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

            public override bool RespondsToSacrifice()
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
            // Build the boon as FullBoon.
            BoonManager.FullBoon example_boon = new BoonManager.FullBoon();

            // Pass the BoonData to the API.
            BoonManager.New<ExampleBoonBehaviour>(
                    PluginGuid,
                    "Example Boon",
                    "This example boon gives one tooth whenever a card is sacrificed.",
                    TextureHelper.GetImageAsTexture("boonicon_example.png"),
                    TextureHelper.GetImageAsTexture("boon_example.png"),
                    true,
                    true
                );
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is an example tribe. Currently it does not add a totem head.
        private static void AddExampleTribe()
        {
            exampleTribe = TribeManager.Add(
                PluginGuid,
                "tribe_example",
                tribeIcon: TextureHelper.GetImageAsTexture("tribeicon_example.png"),
                appearInTribeChoices: true,
                choiceCardbackTexture: TextureHelper.GetImageAsTexture("card_rewardback_example.png")
            );
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

                // You can also force an emmission.
                base.Card.RenderInfo.forceEmissivePortrait = true;
            }

            // When this card is chosen via a sequencer, run the following method. This example does nothing, but think *Ijaraq*.
            public override void OnCardAddedToDeck()
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

        // Harmony patches need to be static.
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

        // This is an example of using the extended ability triggers added by the API.
        // In this ability we're going to attack ALL bird cards on the board.
        // This ability is not already added to the example bear card, but it can be added the same as a regular ability type.

        // When adding interfaces, the format here is "I" plus the trigger type.
        public class NewExtendedAbility : ExtendedAbilityBehaviour, IGetOpposingSlots
        {
            public override Ability Ability
            {
                get
                {
                    return ability;
                }
            }

            public static Ability ability;

            bool IGetOpposingSlots.RespondsToGetOpposingSlots()
            {
                return true;
            }

            // This removes the directly opposing slot from the list of slots to be attacked.
            bool IGetOpposingSlots.RemoveDefaultAttackSlot()
            {
                return true;
            }

            List<CardSlot> IGetOpposingSlots.GetOpposingSlots(List<CardSlot> originalSlots, List<CardSlot> otherAddedSlots)
            {
                List<CardSlot> QueuedSlots = new List<CardSlot>();
                foreach (CardSlot slot in Singleton<BoardManager>.Instance.AllSlots)
                {
                    if (slot.Card && slot.Card.IsOfTribe(Tribe.Bird))
                    {
                        QueuedSlots.Add(slot);
                    }
                }
                return QueuedSlots;
            }

        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------

        // This is your special ability class. This defines what your special ability does.
        public class NewTestSpecialAbility : SpecialCardBehaviour
        {
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

        // Add a custom card cost. This is done by adding to the list of costs in Part1CardCostRender.
        private void AddCustomCosts()
        {
            Part1CardCostRender.UpdateCardCost += delegate (CardInfo card, List<Texture2D> costs)
            {

                // The extended property here simplifies a card-specific cost. 
                // You can also use card.GetExtendedPropertyAsInt("example_cost") elsewhere.
                int? example_cost = card.GetExtendedPropertyAsInt("example_cost");

                if (example_cost > 0)
                {
                    // Add this cost to the card's costs as a texture.
                    // This uses string interpolation (denoted by $) to convey to the texturehelper which number to append to the icon's name.
                    costs.Add(TextureHelper.GetImageAsTexture($"cost_{example_cost}example.png"));
                }

            };
        }

        // This is the actual logic for the custom cost.
        // Here you should perform whatever checks you need to do meet your cost requirements.
        public static bool CheckCustomCost(PlayableCard card)
        {
            if (RunState.Run.playerLives >= card.Info.GetExtendedPropertyAsInt("example_cost"))
            {
                return true;
            }

            return false;
        }

        // This logic determines if the cost is met.
        [HarmonyPatch(typeof(PlayableCard), "CanPlay")]
        [HarmonyPostfix]
        public static void IsCustomCostMet(ref bool __result, ref PlayableCard __instance)
        {

            // Only run this particular logic when applicable.
            if (__instance.Info.GetExtendedPropertyAsInt("example_cost") > 0)
            {
                bool example_cost_met = CheckCustomCost(__instance);

                if (__result == true && example_cost_met == true)
                {
                    __result = true;
                }

                else
                {
                    __result = false;
                }
            }

        }

        // This logic actually applies the cost.
        // This is a very simple example.
        [HarmonyPatch(typeof(PlayerHand), "SelectSlotForCard")]
        [HarmonyPostfix]
        public static IEnumerator PayCustomCost(IEnumerator enumerator, PlayerHand __instance, PlayableCard card)
        {
            if (card.Info.GetExtendedPropertyAsInt("example_cost") > 0)
            {
                // Remove 1 player life.
                RunState.Run.playerLives--;
                Singleton<CandleHolder>.Instance.BlowOutCandle(1);
            }

            return enumerator;
        }

        // This patches the dialogue that occurs when a cost is not affordable.
        [HarmonyPatch(typeof(HintsHandler), "OnNonplayableCardClicked")]
        [HarmonyPostfix]
        public static void ExampleCostCannotAffordHint(ref PlayableCard card)
        {
            if (card.Info.GetExtendedPropertyAsInt("example_cost") > 0 && !CheckCustomCost(card))
            {
                Singleton<TextDisplayer>.Instance.ShowMessage("You only have one candle left...");

                new WaitForSeconds(0.4f);
                Singleton<TextDisplayer>.Instance.Clear();
            }

            else
            {
                return;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------
        /*

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
            .SetCost(bloodCost: 0)

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
            .SetStatIcon(ExampleStatIconID)

            // The first tribe is a custom tribe, we set this earlier and now we pass it to the card.
            // The second tribe example here is vanilla, in the format of Tribe.tribename.
            .AddTribes(new Tribe[] { exampleTribe, Tribe.Canine })

            // Set an extended property here.
            // An extended property can be arbitrary, but in this case we're setting the name of the property to "example_cost" and setting it as an int.
            // This is mainly used for purposes of rendering the custom cost, but this int can also be compared to in the custom cost logic.
            .SetExtendedProperty("example_cost", 2)

            ;

            // Pass the card to the API.
            CardManager.Add(PluginPrefix, EightFuckingBears);
        
    }
        
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

        */
    }
}