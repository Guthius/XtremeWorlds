using Client.Game.UI;
using Client.Net;
using Core;
using Core.Globals;
using Core.Net;
using Type = Core.Globals.Type;

namespace Client
{

    public class Shop
    {
        public static void OnClose()
        {
            Sender.SendCloseShop();
            WindowManager.HideWindow(WindowManager.GetWindowIndex("winShop"));
            WindowManager.HideWindow(WindowManager.GetWindowIndex("winDescription"));
            GameState.ShopSelectedSlot = 0;
            GameState.ShopSelectedItem = 0;
            GameState.ShopIsSelling = false;
            GameState.InShop = -1;
        }

        #region Database

        public static void OnClear(int index)
        {
            Data.Shop[index] = default;
            Data.Shop[index].Name = "";
            Data.Shop[index].TradeItem = new Type.TradeItem[Variables.MaxTrades];
            for (int x = 0; x < Variables.MaxTrades; x++)
            {            
                Data.Shop[index].TradeItem[x].Item = -1;
                Data.Shop[index].TradeItem[x].CostItem = - 1;
            }
            GameState.ShopLoaded[index] = 0;
        }

        public static void OnClearAll()
        {
            int i;

            Data.Shop = new Type.Shop[Variables.MaxShops];

            for (i = 0; i < Variables.MaxShops; i++)
                OnClear(i);

        }

        public static void OnStream(int shopNum)
        {
            if (shopNum >= 0 && string.IsNullOrEmpty(Data.Shop[shopNum].Name) && GameState.ShopLoaded[shopNum] == 0)
            {
                GameState.ShopLoaded[shopNum] = 1;
                SendRequestShop(shopNum);
            }
        }

        #endregion

        #region Incoming Packets

        public static void Packet_OpenShop(ReadOnlyMemory<byte> data)
        {
            int shopnum;
            var buffer = new PacketReader(data);

            shopnum = buffer.ReadInt32();

            GameLogic.OpenShop(shopnum);
        }

        public static void Packet_ResetShopAction(ReadOnlyMemory<byte> data)
        {
            GameState.ShopAction = 0;
        }

        public static void Packet_UpdateShop(ReadOnlyMemory<byte> data)
        {
            int shopnum;
            var buffer = new PacketReader(data);
            shopnum = buffer.ReadInt32();

            Data.Shop[shopnum].BuyRate = buffer.ReadInt32();
            Data.Shop[shopnum].Name = buffer.ReadString();

            for (int i = 0; i < Variables.MaxTrades; i++)
            {
                Data.Shop[shopnum].TradeItem[i].CostItem = buffer.ReadInt32();
                Data.Shop[shopnum].TradeItem[i].CostValue = buffer.ReadInt32();
                Data.Shop[shopnum].TradeItem[i].Item = buffer.ReadInt32();
                Data.Shop[shopnum].TradeItem[i].ItemValue = buffer.ReadInt32();
            }

            if (Data.Shop[shopnum].Name is null)
                Data.Shop[shopnum].Name = "";
        }

        #endregion

        #region Outgoing Packets

        public static void SendRequestShop(int shopNum)
        {
            var packetWriter = new PacketWriter(8);

            packetWriter.WriteEnum(Packets.ClientPackets.CRequestShop);
            packetWriter.WriteInt32(shopNum);

            Network.Send(packetWriter);
        }

        public static void BuyItem(int shopSlot)
        {
            var packetWriter = new PacketWriter(8);

            packetWriter.WriteEnum(Packets.ClientPackets.CBuyItem);
            packetWriter.WriteInt32(shopSlot);

            Network.Send(packetWriter);
        }

        public static void SellItem(int invslot)
        {
            var packetWriter = new PacketWriter(8);

            packetWriter.WriteEnum(Packets.ClientPackets.CSellItem);
            packetWriter.WriteInt32(invslot);

            Network.Send(packetWriter);
        }

        #endregion

    }
}