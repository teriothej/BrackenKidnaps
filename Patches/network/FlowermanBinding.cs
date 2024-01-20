﻿using GameNetcodeStuff;
using SnatchinBracken.Patches.data;
using Unity.Netcode;
using UnityEngine;

namespace SnatchingBracken.Patches.network
{
    public class FlowermanBinding : NetworkBehaviour
    {

        [ServerRpc(RequireOwnership = false)]
        public void BindPlayerServerRpc(int playerId, ulong flowermanId)
        {
            AddBindingsClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnbindPlayerServerRpc(int playerId, ulong flowermanId)
        {
            RemoveBindingsClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetEntityStatesServerRpc(int playerId, ulong flowermanId)
        {
            ResetEntityStatesClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PrepForBindingServerRpc(int playerId, ulong flowermanId)
        {
            PrepForBindingClientRpc(playerId, flowermanId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateFavoriteSpotServerRpc(int playerId, ulong flowermanId)
        {
            UpdateFavoriteSpotClientRpc(playerId, flowermanId);
        }

        [ClientRpc]
        public void ResetEntityStatesClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

            player.inSpecialInteractAnimation = false;
            player.inAnimationWithEnemy = null;

            flowermanAI.carryingPlayerBody = false;
            flowermanAI.creatureAnimator.SetBool("killing", value: false);
            flowermanAI.creatureAnimator.SetBool("carryingBody", value: false);
            flowermanAI.stunnedByPlayer = null;
            flowermanAI.stunNormalizedTimer = 0f;
            flowermanAI.angerMeter = 0f;
            flowermanAI.isInAngerMode = false;
            flowermanAI.timesThreatened = 0;
            flowermanAI.evadeStealthTimer = 0.1f;
            // little did i know this one is extremely important
            flowermanAI.isClientCalculatingAI = false;
            flowermanAI.favoriteSpot = null;
            flowermanAI.FinishKillAnimation(false);
        }

        [ClientRpc]
        public void PrepForBindingClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

            flowermanAI.creatureAnimator.SetBool("killing", value: false);
            flowermanAI.creatureAnimator.SetBool("carryingBody", value: true);
            flowermanAI.carryingPlayerBody = true;

            player.inSpecialInteractAnimation = true;
            player.inAnimationWithEnemy = flowermanAI;

            flowermanAI.inKillAnimation = false;
            flowermanAI.targetPlayer = null;
        }

        [ClientRpc]
        public void UpdateFavoriteSpotClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

            Transform transform = flowermanAI.ChooseFarthestNodeFromPosition(player.transform.position);
            flowermanAI.favoriteSpot = transform;
        }

        [ClientRpc]
        public void AddBindingsClientRpc(int playerId, ulong flowermanId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanId];

            SharedData.Instance.BindedDrags[flowermanAI] = player;
            SharedData.Instance.PlayerIDs[player] = playerId;
            SharedData.Instance.IDsToPlayerController[playerId] = player;
            SharedData.Instance.LastGrabbedTimeStamp[flowermanAI] = Time.time;
        }

        [ClientRpc]
        public void RemoveBindingsClientRpc(int playerId, ulong flowermanID)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            FlowermanAI flowermanAI = SharedData.Instance.FlowermanIDs[flowermanID];

            SharedData.Instance.BindedDrags.Remove(flowermanAI);
            SharedData.Instance.LastGrabbedTimeStamp[flowermanAI] = Time.time;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
    }
}