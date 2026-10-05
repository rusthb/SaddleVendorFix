using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info( "SaddleVendorFix", "RustMaps & Jexs", "1.0.0" )]
    [Description( "Scans for any saddle vendors missing their invisible vending machine references and fixes them." )]
    internal class SaddleVendorFix : CovalencePlugin
    {
        private const string StablesShopKeeperPrefab = "assets/prefabs/npc/bandit/shopkeepers/stables_shopkeeper.prefab";
        private const string InvisibleVendingMachinePrefab = "assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab";
        
        private void OnServerInitialized()
        {
            foreach (var shopKeeper in BaseNetworkable.serverEntities.OfType<NPCShopKeeper>())
            {
                if (shopKeeper is null || 
                    shopKeeper.invisibleVendingMachineRef.IsValid(true) || 
                    shopKeeper.PrefabName != StablesShopKeeperPrefab)
                {
                    continue;
                }

                var transform = shopKeeper.transform;
                if (GameManager.server.CreateEntity(
                        InvisibleVendingMachinePrefab, 
                        transform.position + new Vector3(0f, -1.5f, 0f) + transform.forward * 1.5f, 
                        transform.rotation,
                        false) 
                    is not InvisibleVendingMachine invisibleVendingMachine)
                {
                    LogWarning($"Unable to create the vending machine for the shop keeper at {transform.position.ToString()}. Please contact plugin developer(s).");
                    continue;
                }
			
                Puts($"Found a saddle vendor at {transform.position.ToString()} with a missing invisible vending machine. Attempting to fix...");
			
                invisibleVendingMachine.Spawn();
                invisibleVendingMachine.EnableSaving(false);
			
                using (var flags = invisibleVendingMachine.StartSetFlags(BaseEntity.FlagsUpdateMode.SendNetworkUpdate_Flags))
                {
                    flags.Set(VendingMachine.VendingMachineFlags.EmptyInv, true);
                    flags.Set(VendingMachine.VendingMachineFlags.Broadcasting, true);
                }
                invisibleVendingMachine.UpdateMapMarker();
			
                invisibleVendingMachine.vendingOrders = invisibleVendingMachine.vmoManifest.GetFromIndex(18);
                invisibleVendingMachine.InstallFromVendingOrders();
                invisibleVendingMachine.SendNetworkUpdateImmediate();
			
                // Special thanks to lencorp on Discord for helping me get this part working again after a Rust update.
                invisibleVendingMachine.SetAttachedNPC(shopKeeper);
                shopKeeper.invisibleVendingMachineRef.Set(invisibleVendingMachine);
			
                Puts("Fix applied!");
            }
        }
    }
}