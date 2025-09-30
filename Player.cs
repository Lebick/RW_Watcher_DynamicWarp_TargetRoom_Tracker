using BepInEx;
using BepInEx.Logging;
using Expedition;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace LevicksSeedTest;

public partial class LevicksSeedTest
{
    public static string currentRoom = "";

    public static UIContainer container;
    public static List<string> normalMapList = new();
    public static List<ListItem> normalRegs = new();

    public static List<string> badMapList = new();
    public static List<ListItem> badRegs = new();

    private static new ManualLogSource Logger;

    private void SetInit(ManualLogSource logger)
    {
        Logger = logger;
        On.Menu.PauseMenu.ctor += PauseMenu_ctor;
    }

    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        orig.Invoke(self, manager, game);

        container = new UIContainer(self, self.pages[0], self.pages[0].pos, new Vector2());
        self.pages[0].subObjects.Add(container);

    }

    private static List<ListItem> GetNormalAbleTargets(RainWorldGame game)
    {
        int seed = game.rainWorld.progression.miscProgressionData.watcherCampaignSeed;

        AbstractRoom room = game.Players[0].Room;

        normalMapList = Watcher.WarpPoint.GetAvailableDynamicWarpTargets(room.world, room.name, null, false);

        List<string> allowLists = new();

        foreach (var content in normalMapList)
            if (!allowLists.Contains(content))
                allowLists.Add(content);

        List<ListItem> returnValue = new();

        for (int i = 0; i < allowLists.Count; i++)
        {
            returnValue.Add(new ListItem(allowLists[i], i));
            Logger.LogDebug($"NormalWarps Added : {allowLists[i]}");
        }

        return returnValue;
    }

    private static List<ListItem> GetBadAbleTargets(RainWorldGame game)
    {
        int seed = game.rainWorld.progression.miscProgressionData.watcherCampaignSeed;

        AbstractRoom room = game.Players[0].Room;

        badMapList = Watcher.WarpPoint.GetAvailableBadWarpTargets(room.world, room.name);

        List<string> allowLists = new();

        foreach (var content in badMapList)
            if (!allowLists.Contains(content))
                allowLists.Add(content);

        List<ListItem> returnValue = new();

        for (int i = 0; i < allowLists.Count; i++)
        {
            returnValue.Add(new ListItem(allowLists[i], i));
            Logger.LogDebug($"BadWarps Added : {allowLists[i]}");
        }

        return returnValue;
    }

    private static string Translator(string value)
    {
        return Custom.rainWorld.inGameTranslator.Translate(value);
    }

    public class UIContainer : RectangularMenuObject
    {
        public RainWorldGame game;
        public SimpleButton toggleButton;
        public MenuTabWrapper tabWrapper;
        public OpComboBox roomDropDown;
        public OpComboBox badRoomDropdown;
        public MenuLabel desc2;
        public MenuLabel desc3;
        public MenuLabel seedLabel;
        public string seedText;
        public Configurable<string> selectRoom;

        public int currentSeed = 0;
        public int normalWarpCreateCount = 0;
        public int badWarpCreateCount = 0;

        public bool isShowBadWarpMenu;

        public float yOffset;

        public UIContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            isShowBadWarpMenu = false;

            game = (menu as PauseMenu).game;

            currentSeed = game.rainWorld.progression.miscProgressionData.watcherCampaignSeed;
            normalWarpCreateCount = game.GetStorySession.saveState.miscWorldSaveData.numberOfWarpPointsGenerated;
            badWarpCreateCount = game.GetStorySession.saveState.miscWorldSaveData.numberOfBadWarpsGenerated;

            currentRoom = game.Players[0].Room.name;
            selectRoom = new Configurable<string>(currentRoom);

            normalRegs = GetNormalAbleTargets(game);
            badRegs = GetBadAbleTargets(game);
            GetSeedText(normalMapList, normalRegs[0].name, normalWarpCreateCount);

            yOffset = 0;

            #region 타이틀

            //TurnNormal , TurnBad
            toggleButton = new SimpleButton(menu, this, $"{Translator("TurnBad")}", "badToggle", new Vector2(10f, game.rainWorld.options.ScreenSize.y - 40f), new Vector2(80f, 20f));
            subObjects.Add(toggleButton);

            MenuLabel desc1 = new MenuLabel(menu, this, $"{Translator("WarpAblePositions")}", new Vector2(10f, game.rainWorld.options.ScreenSize.y - 65f), new Vector2(), false);
            desc1.label.alignment = FLabelAlignment.Left;

            subObjects.Add(desc1);

            #endregion

            #region 기타

            tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);

            roomDropDown = new OpComboBox(selectRoom, new Vector2(10f, desc1.pos.y - 40f), 150f, normalRegs);
            roomDropDown.listHeight = 29;
            roomDropDown.OnValueChanged += RoomDropDown_OnValueChanged;
            roomDropDown.OnListOpen += RoomDropDown_OnListOpen;
            roomDropDown.OnListClose += RoomDropDown_OnListClose;
            UIelementWrapper wrapper = new UIelementWrapper(tabWrapper, roomDropDown);

            tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);

            SimpleButton copyButton = new SimpleButton(menu, this, $"{Translator("SeedCopy")}", "Copy", new Vector2(200f, roomDropDown.PosY), new Vector2(100f, 30f));
            subObjects.Add(copyButton);

            desc2 = new MenuLabel(menu, this, $"{Translator("normalDynamicTargetRoom")} = {GetNormalDynamicTarget()}", new Vector2(10f, roomDropDown.PosY - 50 - yOffset), new Vector2(), false);
            desc2.label.alignment = FLabelAlignment.Left;
            subObjects.Add(desc2);

            #endregion

            #region 시드

            string seedText = $"{Translator("currentSeed")} = {currentSeed}\n  > {(
                isShowBadWarpMenu ? $"{Translator("badWarpSeed")} = {currentSeed + badWarpCreateCount}"
                : $"{Translator("normalWarpSeed")} = {currentSeed + normalWarpCreateCount}")}";

            seedLabel = new MenuLabel(
                menu,
                this, 
                seedText,
                new Vector2(10f, desc2.pos.y - 50 - yOffset),
                new Vector2(),
                false);

            seedLabel.label.alignment = FLabelAlignment.Left;


            subObjects.Add(seedLabel);
            

            #endregion
        }

        private void OnChangedViewState()
        {
            isShowBadWarpMenu = !isShowBadWarpMenu;

            List<string> values = new();
            ListItem[] items = roomDropDown.GetItemList();
            foreach (var reg in items)
            {
                values.Add(reg.displayName);
            }

            if (isShowBadWarpMenu)
            {
                roomDropDown.AddItems(true, badRegs.ToArray());
                roomDropDown.RemoveItems(true, values.ToArray());

                desc2.text = $"{Translator("badDynamicTargetRoom")} = {GetBadDynamicTarget()}";

                toggleButton.menuLabel.text = $"{Translator("TurnNormal")}";
            }
            else
            {
                roomDropDown.AddItems(true, normalRegs.ToArray());
                roomDropDown.RemoveItems(true, values.ToArray());

                desc2.text = $"{Translator("normalDynamicTargetRoom")} = {GetNormalDynamicTarget()}";

                toggleButton.menuLabel.text = $"{Translator("TurnBad")}";
            }

            string seedText = $"{Translator("currentSeed")} = {currentSeed}\n  > {(
                isShowBadWarpMenu ? $"{Translator("badWarpSeed")} = {currentSeed + badWarpCreateCount}"
                : $"{Translator("normalWarpSeed")} = {currentSeed + normalWarpCreateCount}")}";

            seedLabel.text = seedText;
        }

        private void RoomDropDown_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            if (isShowBadWarpMenu)
            {
                GetSeedText(badMapList, value, badWarpCreateCount);
                desc2.text = $"{Translator("badDynamicTargetRoom")} = {GetBadDynamicTarget()}";
            }
            else
            {
                GetSeedText(normalMapList, value, normalWarpCreateCount);
                desc2.text = $"{Translator("normalDynamicTargetRoom")} = {GetNormalDynamicTarget()}";
            }
        }

        private void RoomDropDown_OnListOpen(UIfocusable trigger)
        {
            yOffset = 3000;

            desc2.pos = new Vector2(10f, roomDropDown.PosY - 50 - yOffset);
            seedLabel.pos = new Vector2(10f, desc2.pos.y - 50 - yOffset);
        }

        private void RoomDropDown_OnListClose(UIfocusable trigger)
        {
            yOffset = 0;

            desc2.pos = new Vector2(10f, roomDropDown.PosY - 50 - yOffset);
            seedLabel.pos = new Vector2(10f, desc2.pos.y - 50 - yOffset);
        }

        private string GetNormalDynamicTarget()
        {
            UnityEngine.Random.State wasState = UnityEngine.Random.state;

            UnityEngine.Random.InitState(currentSeed + normalWarpCreateCount);
            string map = normalMapList[UnityEngine.Random.Range(0, normalMapList.Count)];

            UnityEngine.Random.state = wasState;
            return map;
        }

        private string GetBadDynamicTarget()
        {
            UnityEngine.Random.State wasState = UnityEngine.Random.state;

            UnityEngine.Random.InitState(currentSeed + badWarpCreateCount);
            string map = badMapList[UnityEngine.Random.Range(0, badMapList.Count)];

            UnityEngine.Random.state = wasState;
            return map;
        }

        private void GetSeedText(List<string> mapList, string value, int addValue = 0)
        {
            UnityEngine.Random.State wasState = UnityEngine.Random.state;

            var sb = new System.Text.StringBuilder();
            int count = 0;

            for (int i = 0 + addValue; i < 100000 + addValue; i++)
            {
                UnityEngine.Random.InitState(i);
                int index = UnityEngine.Random.Range(0, mapList.Count);

                if (value == mapList[index])
                {
                    count++;
                    sb.Append(i - addValue);
                    sb.Append(", ");

                    if (count % 20 == 0)
                    {
                        sb.Append("\r\n");
                    }
                }
            }

            seedText = sb.ToString();

            UnityEngine.Random.state = wasState;
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);

            if (message == "Copy")
            {
                Logger.LogMessage("copied seed");

                GUIUtility.systemCopyBuffer = seedText;
            }

            if(message == "badToggle")
            {
                OnChangedViewState();
            }
        }
    }
}

