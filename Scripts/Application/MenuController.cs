using Godot;
using System;

namespace Abyss.Application;
internal sealed class MenuController
{
    private readonly IGameHost host;
    private readonly InventoryService inventoryService;
    private readonly InventoryState inventoryState;
    private readonly JournalFormatter journalFormatter;
    private readonly LanguagePreferences languagePreferences;
    private readonly MenuState menuState;
    private readonly MerchantService merchantService;
    private readonly PlayerActions playerActions;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    private readonly IRunLifecycle lifecycle;
    internal MenuController(IGameHost host, InventoryService inventoryService, InventoryState inventoryState, JournalFormatter journalFormatter, LanguagePreferences languagePreferences, MenuState menuState, MerchantService merchantService, PlayerActions playerActions, PlayerState playerState, RunState runState, IRunLifecycle lifecycle)
    {
        this.host = host;
        this.inventoryService = inventoryService;
        this.inventoryState = inventoryState;
        this.journalFormatter = journalFormatter;
        this.languagePreferences = languagePreferences;
        this.menuState = menuState;
        this.merchantService = merchantService;
        this.playerActions = playerActions;
        this.playerState = playerState;
        this.runState = runState;
        this.lifecycle = lifecycle;
    }

    internal void HandlePause(Key key)
    {
        if (key == Key.Escape)
        {
            runState.Screen = "game";
            return;
        }

        if (key == Key.I)
        {
            menuState.PauseTab = 1;
            return;
        }

        int previous = menuState.PauseTab;
        if (key == Key.Left || key == Key.A)
            menuState.PauseTab = (menuState.PauseTab + 4) % 5;
        if (key == Key.Right || key == Key.D || key == Key.Tab)
            menuState.PauseTab = (menuState.PauseTab + 1) % 5;
        if (key >= Key.Key1 && key <= Key.Key5)
            menuState.PauseTab = (int)key - (int)Key.Key1;
        if (previous != menuState.PauseTab)
        {
            menuState.MenuIndex = 0;
            return;
        }

        if (menuState.PauseTab == 1)
            HandleInventory(key);
        if (menuState.PauseTab == 2)
        {
            int pages = Math.Max(1, (journalFormatter.JournalLines().Count + UiTheme.JournalPageSize - 1) / UiTheme.JournalPageSize);
            if (UiTheme.Previous(key))
                menuState.JournalPage = Math.Max(0, menuState.JournalPage - 1);
            if (UiTheme.Next(key))
                menuState.JournalPage = Math.Min(pages - 1, menuState.JournalPage + 1);
            if (key == Key.Home)
                menuState.JournalPage = 0;
            if (key == Key.End)
                menuState.JournalPage = pages - 1;
        }

        if (menuState.PauseTab == 3)
            HandleHelp(key);
        if (menuState.PauseTab == 4)
        {
            if (UiTheme.Previous(key) || UiTheme.Next(key))
                menuState.MenuIndex = 1 - menuState.MenuIndex;
            if (UiTheme.Confirm(key))
            {
                if (menuState.MenuIndex == 0)
                    OpenLanguage("pause");
                else
                    AskReturnToMenu();
            }
        }
    }

    internal void OpenLanguage(string from)
    {
        menuState.LanguageReturn = from;
        menuState.LanguageIndex = menuState.English ? 1 : 0;
        runState.Screen = "language";
    }

    internal bool HandleMenus(Key key)
    {
        if (runState.Screen == "game")
            return false;
        if (runState.Screen == "home")
        {
            if (UiTheme.Previous(key))
                menuState.MenuIndex = (menuState.MenuIndex + 2) % 3;
            if (UiTheme.Next(key))
                menuState.MenuIndex = (menuState.MenuIndex + 1) % 3;
            if (key == Key.Key1)
                menuState.MenuIndex = 0;
            if (key == Key.Key2)
                menuState.MenuIndex = 1;
            if (key == Key.Key3)
                menuState.MenuIndex = 2;
            if (UiTheme.Confirm(key))
            {
                if (menuState.MenuIndex == 0)
                    runState.Screen = "classes";
                else if (menuState.MenuIndex == 1)
                    OpenLanguage("home");
                else
                {
                    menuState.ExitYes = false;
                    runState.Screen = "confirm_quit";
                }
            }
        }
        else if (runState.Screen == "language")
        {
            if (UiTheme.Previous(key) || UiTheme.Next(key) || key == Key.Left || key == Key.Right)
                menuState.LanguageIndex = 1 - menuState.LanguageIndex;
            if (key == Key.Key1)
                menuState.LanguageIndex = 0;
            if (key == Key.Key2)
                menuState.LanguageIndex = 1;
            if (UiTheme.Confirm(key))
            {
                menuState.English = menuState.LanguageIndex == 1;
                languagePreferences.SaveLanguage();
                runState.Screen = menuState.LanguageReturn;
                menuState.MenuIndex = 0;
            }

            if (key == Key.Escape)
            {
                runState.Screen = menuState.LanguageReturn;
                menuState.MenuIndex = 0;
            }
        }
        else if (runState.Screen == "classes")
        {
            if (key >= Key.Key1 && key <= Key.Key4)
                playerState.ClassIndex = (int)key - (int)Key.Key1;
            if (key == Key.Left || key == Key.A)
                playerState.ClassIndex = (playerState.ClassIndex + 3) % 4;
            if (key == Key.Right || key == Key.D)
                playerState.ClassIndex = (playerState.ClassIndex + 1) % 4;
            if (UiTheme.Confirm(key))
                lifecycle.Start();
            if (key == Key.Escape)
            {
                runState.Screen = "home";
                menuState.MenuIndex = 0;
            }
        }
        else if (runState.Screen == "pause")
            HandlePause(key);
        else if (runState.Screen == "shop")
            HandleShop(key);
        else if (runState.Screen == "confirm_quit")
        {
            if (key == Key.Escape) runState.Screen = "home";
            else if (UiTheme.Previous(key) || UiTheme.Next(key) || key == Key.Left || key == Key.Right)
                menuState.ExitYes = !menuState.ExitYes;
            else if (UiTheme.Confirm(key))
            {
                if (menuState.ExitYes) host.Quit();
                else runState.Screen = "home";
            }
        }
        else if (runState.Screen == "confirm_exit")
            HandleExitConfirm(key);
        else if (runState.Screen == "dead")
        {
            if (UiTheme.Confirm(key))
            {
                runState.Screen = "home";
                menuState.MenuIndex = 0;
            }
        }

        return true;
    }

    internal void HandleHelp(Key key)
    {
        if (UiTheme.Previous(key))
            menuState.HelpTopic = (menuState.HelpTopic + 8) % 9;
        if (UiTheme.Next(key))
            menuState.HelpTopic = (menuState.HelpTopic + 1) % 9;
    }

    internal void HandleShop(Key key)
    {
        if (menuState.PendingTrade != null)
        {
            if (key == Key.Escape)
            {
                menuState.PendingTrade = null;
                return;
            }

            if (UiTheme.Previous(key) || UiTheme.Next(key) || key == Key.Left || key == Key.Right)
                menuState.ConfirmYes = !menuState.ConfirmYes;
            if (UiTheme.Confirm(key))
            {
                if (menuState.ConfirmYes)
                    merchantService.CompleteTrade();
                else
                    menuState.PendingTrade = null;
            }

            return;
        }

        if (key == Key.Escape)
        {
            runState.Screen = "game";
            return;
        }

        if (key == Key.Left || key == Key.Right || key == Key.A || key == Key.D || key == Key.Tab)
        {
            menuState.ShopSelling = !menuState.ShopSelling;
            menuState.ShopIndex = 0;
            menuState.ShopNotice = ("", "");
        }

        var offers = merchantService.ShopOffers();
        if (offers.Count == 0)
            return;
        if (UiTheme.Previous(key))
            menuState.ShopIndex = (menuState.ShopIndex + offers.Count - 1) % offers.Count;
        if (UiTheme.Next(key))
            menuState.ShopIndex = (menuState.ShopIndex + 1) % offers.Count;
        menuState.ShopIndex = Math.Clamp(menuState.ShopIndex, 0, offers.Count - 1);
        if (UiTheme.Confirm(key))
        {
            menuState.PendingTrade = offers[menuState.ShopIndex];
            menuState.ConfirmYes = false;
        }
    }

    internal void AskReturnToMenu()
    {
        menuState.ExitYes = false;
        runState.Screen = "confirm_exit";
    }

    internal void HandleExitConfirm(Key key)
    {
        if (key == Key.Escape)
        {
            runState.Screen = "pause";
            return;
        }

        if (UiTheme.Previous(key) || UiTheme.Next(key) || key == Key.Left || key == Key.Right)
            menuState.ExitYes = !menuState.ExitYes;
        if (UiTheme.Confirm(key))
        {
            runState.Screen = menuState.ExitYes ? "home" : "pause";
            if (menuState.ExitYes)
                menuState.MenuIndex = 0;
        }
    }

    internal void HandleInventory(Key key)
    {
        int count = 7 + inventoryState.Backpack.Count;
        if (UiTheme.Previous(key))
            menuState.InventoryIndex = (menuState.InventoryIndex + count - 1) % count;
        if (UiTheme.Next(key))
            menuState.InventoryIndex = (menuState.InventoryIndex + 1) % count;
        if (key == Key.T && menuState.InventoryIndex is 3 or 6) { playerActions.BeginTorchThrow(); return; }
        if (!UiTheme.Confirm(key))
            return;
        if (menuState.InventoryIndex < 3)
        {
            if (inventoryState.Equipped[menuState.InventoryIndex] is Gear g)
                inventoryService.ToggleGear(g);
            else
                inventoryService.InventoryMessage("Slot vazio. Selecione um item na mochila.", "Empty slot. Select an item in the backpack.");
        }
        else if (menuState.InventoryIndex is 3 or 6)
            playerActions.ToggleTorch();
        else if (menuState.InventoryIndex == 4)
        {
            if (inventoryState.Potions == 0 || playerState.Health == playerState.MaxHealth)
            {
                inventoryService.InventoryMessage("Sem pocao ou vida ja cheia.", "No potion or health already full.");
                return;
            }

            runState.Screen = "game";
            playerActions.Drink();
        }
        else if (menuState.InventoryIndex == 5)
            inventoryService.DrinkEnergy();
        else
            inventoryService.ToggleGear(inventoryState.Backpack[menuState.InventoryIndex - 7]);
    }
}
