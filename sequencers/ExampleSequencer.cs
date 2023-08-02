using BepInEx.Logging;
using System;
using System.Reflection;
using HarmonyLib;
using InscryptionAPI;
using BepInEx;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DiskCardGame;
using UnityEngine;
using InscryptionAPI.Saves;
using InscryptionAPI.Card;
using InscryptionAPI.Ascension;
using InscryptionAPI.Helpers;
using InscryptionAPI.Encounters;
using InscryptionAPI.Nodes;

using System.Linq;

namespace ExampleMod;

// This example sequence simply lowers the blood cost of a valid card in the player deck by 1.
public class ExampleSequencer : CustomNodeSequencer, ICustomNodeSequencer
{
	public SelectCardFromDeckSlot selectionSlot = SpecialNodeHandler.Instance.cardStatBoostSequencer.selectionSlot;

    public override bool ShouldNotReturnToMapOnEnd(CustomSpecialNodeData exampleNode)
    {
        return true;
    }

    public override IEnumerator DoCustomSequence(CustomSpecialNodeData exampleNode)
	{
		Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, true);
		yield return new WaitForSeconds(0.5f);

		Singleton<ViewManager>.Instance.SwitchToView(View.StatBoostSlot, false, true);

		// Make the RuleBook visible on the table.
		Singleton<TableRuleBook>.Instance.SetOnBoard(true);

		// Enable the mouse.
		InteractionCursor.Instance.SetEnabled(true);

		// We make a reference to the cardStatBoostSequencer.selectionSlot.
		selectionSlot.ClearDelegates();

		// This removes the fire animations from the slot.
		foreach (Component child in selectionSlot.GetComponentsInChildren<Component>())
		{
			if (child && child.ToString().Contains("FireAnim"))
			{
				Destroy(child.gameObject);
			}
		};

		// This clears the fire animation component from the slot.
		if (selectionSlot.specificRenderers.Count > 1)
		{
			selectionSlot.specificRenderers.RemoveAt(1);
		}

		// We're overwriting the slot texture with our own, here.
		selectionSlot.specificRenderers[0].material.mainTexture = TextureHelper.GetImageAsTexture("slot_example_sequence.png");

		// We want to give our selectionSlot a CursorSelectStarted action.
		SelectCardFromDeckSlot selectCardFromDeckSlot = this.selectionSlot;
		selectCardFromDeckSlot.CursorSelectStarted = (Action<MainInputInteractable>)
		Delegate.Combine(
			selectCardFromDeckSlot.CursorSelectStarted,
			new Action<MainInputInteractable>(delegate (MainInputInteractable i)
				{
					OnSlotSelected(i);
				}
				
			)
		);

		yield return new WaitForSeconds(0.3f);

		// We can make the room dark by turning off the lights.
		Singleton<ExplorableAreaManager>.Instance.HangingLight.gameObject.SetActive(false);
		Singleton<ExplorableAreaManager>.Instance.HandLight.gameObject.SetActive(false);

		// Speech for flavor.
		yield return TextDisplayer.Instance.ShowUntilInput("There is nothing here yet...", -0.65f, emotion: Emotion.Laughter, speaker: DialogueEvent.Speaker.Single);
		yield return TextDisplayer.Instance.ShowUntilInput("Pity.", -0.45f, emotion: Emotion.Anger, speaker: DialogueEvent.Speaker.Leshy);

		// While the lights are out, activate and show our selectionSlot.
		selectionSlot.gameObject.SetActive(true);
		selectionSlot.RevealAndEnable();

		yield return new WaitForSeconds(0.3f);

		// Turn the lights back on.
		Singleton<ExplorableAreaManager>.Instance.HangingLight.gameObject.SetActive(true);
		Singleton<ExplorableAreaManager>.Instance.HandLight.gameObject.SetActive(true);

		yield return new WaitForSeconds(0.5f);

		// Exclaim about the appeared slot.
		yield return TextDisplayer.Instance.ShowUntilInput("Oh..?", -0.45f, emotion: Emotion.Curious, speaker: DialogueEvent.Speaker.Leshy);

		yield break;

	}

	// On selecting the slot, run the contents of this method.
	private void OnSlotSelected(MainInputInteractable slot)
	{
		List<CardInfo> validCards = GetValidCards();

		// Restrict the player from activating the slot while it's already in use. 
		selectionSlot.SetEnabled(false);
		selectionSlot.ShowState(HighlightedInteractable.State.NonInteractable, false, 0.15f);

		if (validCards == null)
		{
			// There's certain things like WaitForSeconds that don't run in a void method,
			// so we use a coroutine to run an IEnumerator.
			StartCoroutine(ApplyCardCostSequence(false));
		}

		else if (validCards != null)
		{
			(slot as SelectCardFromDeckSlot).SelectFromCards(validCards, new Action(OnSlotSelectionEnded), false);
		}
	}

	// This is where we do the dirty work with our card.
	private IEnumerator ApplyCardCostSequence(bool validCards)
	{

		if (!validCards)
		{
			selectionSlot.SetEnabled(false);

			// No valid cards in the deck, so cancel the sequencer early.
			selectionSlot.FlyOffCard();
			selectionSlot.Disable();
			selectionSlot.SetShown(false);

			SpecialNodeHandler.Instance.cardStatBoostSequencer.selectionSlot.pile.Draw();
			SpecialNodeHandler.Instance.cardStatBoostSequencer.selectionSlot.pile.DestroyCardsImmediate();

			yield return new WaitForSeconds(0.5f);

			if (Singleton<GameFlowManager>.Instance != null)
			{
				Singleton<GameFlowManager>.Instance.TransitionToGameState(GameState.Map, null);
			}

			yield break;
		}

		selectionSlot.SetEnabled(false);

		// Use concatenation to pass cardinfo to speech.
		yield return TextDisplayer.Instance.ShowUntilInput("That's a fine " + selectionSlot.Card.Info.displayedName + "...");

		// We make a cheaper copy of the card on the slot.
		CardInfo modifiedCard = ScriptableObject.CreateInstance<CardInfo>();
		modifiedCard = selectionSlot.Card.Info;
		modifiedCard.cost = selectionSlot.Card.Info.cost - 1;

		// Flip the card facedown, apply the changes, then flip it back up.
		selectionSlot.Card.SetFaceDown(true);
		selectionSlot.Card.Info.cost = modifiedCard.cost;
		selectionSlot.Card.SetInfo(modifiedCard);
		selectionSlot.Card.RenderCard();
		yield return new WaitForSeconds(0.6f);
		selectionSlot.Card.SetFaceDown(false);

		// Remove the old card from the deck, and add the modified one.
		RunState.Run.playerDeck.RemoveCard(selectionSlot.Card.Info);
		RunState.Run.playerDeck.AddCard(modifiedCard);
		SaveManager.SaveToFile(false);

		// Keep the camera on the slot.
		Singleton<ViewManager>.Instance.SwitchToView(View.StatBoostSlot, false, true);

		yield return new WaitForSeconds(1.2f);

		// This calls the animation of the card flying up.
		// Then we disable the slot entirely.
		selectionSlot.FlyOffCard();
		selectionSlot.Disable();
		selectionSlot.SetShown(false);

		// Play the animation of the deck being picked up.
		// Then we destroy the pile.
		SpecialNodeHandler.Instance.cardStatBoostSequencer.selectionSlot.pile.Draw();
		SpecialNodeHandler.Instance.cardStatBoostSequencer.selectionSlot.pile.DestroyCardsImmediate();

		yield return new WaitForSeconds(0.5f);

		// We return to the map.
		bool isGameStateValid = Singleton<GameFlowManager>.Instance != null;
		if (isGameStateValid)
		{
			Singleton<GameFlowManager>.Instance.TransitionToGameState(GameState.Map, null);
		}

		// End this method.
		yield break;
	}

	// Confirm the card selection and run the modification sequence.
	private void OnSlotSelectionEnded()
	{
		bool isValid = selectionSlot.Card != null;

		if (isValid)
		{
			Debug.Log("SlotSelectionEnded");
			selectionSlot.SetShown(true, false);
			selectionSlot.ShowState(HighlightedInteractable.State.Interactable, false, 0.15f);
			Singleton<ViewManager>.Instance.SwitchToView(View.StatBoostSlot, false, true);
			StartCoroutine(ApplyCardCostSequence(true));
		}

	}

	// This method retrieves cards from the player deck that fulfill whatever qualifications we want.
	private List<CardInfo> GetValidCards()
	{
		// Get the player deck as a list we can modify.
		List<CardInfo> list = new List<CardInfo>();
		list.Clear();
		list.AddRange(RunState.DeckList);

		// Remove all cards from the list that do not fulfill qualifications.
		// In this case, since we're reducing the cost of a card by 1 blood, we want to remove any cards without a blood cost.
		list.RemoveAll((CardInfo x) => x.cost == 0);

		// Return the list with only valid cards.
		return list;
	}
}


