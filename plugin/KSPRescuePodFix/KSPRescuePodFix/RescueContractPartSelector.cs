using System;
using System.Collections.Generic;
using UnityEngine;
using Contracts;

namespace SSTUTools
{
    [KSPAddon(KSPAddon.Startup.Instantly | KSPAddon.Startup.EveryScene, true)]
    public class RescueContractPartSelector : MonoBehaviour
    {

        private static string[] approvedPodTypes;
        private static System.Random rng;

        public void Start()
        {
            //persist this addon across scenes
            DontDestroyOnLoad(this);
            GameEvents.Contract.onOffered.Add(OnContractOffered);
            rng = new System.Random();
        }

        public void OnDestroy()
        {
            //technically should be noop, as object should not be destroyed until application exit
            GameEvents.Contract.onOffered.Remove(OnContractOffered);
        }

        /// <summary>
        /// Reload the availalable pod types every time module-manager finishes its loading sequence
        /// This creates a dependency on module-manager for even basic functionality,
        /// but allows for run-time data reloading through the 'reload database' features
        /// </summary>
        public void ModuleManagerPostLoad()
        {
            List<string> typesList = new List<string>();
            ConfigNode[] approvedPods = GameDatabase.Instance.GetConfigNodes("RESCUE_POD_TYPES");
            int len1 = approvedPods.Length;
            for (int i = 0; i < len1; i++)
            {
                string[] types = approvedPods[i].GetValues("part");
                typesList.AddRange(types);
            }
            approvedPodTypes = typesList.ToArray();
        }

        public void OnContractOffered(Contract contract)
        {
            ConfigNode contractData = new ConfigNode("CONTRACT");
            try
            {
                contract.Save(contractData);

                // if partID already assigned, contract is already accepted/generated, as the part already exists
                int partID = contractData.HasValue("partID") ? int.Parse(contractData.GetValue("partID")) : 0;
                if (partID != 0) { return; }

                // only care about kerbal-recovery contracts where they spawn in the pod
                // which from experimentation, is type == 1
                int type = contractData.HasValue("recoveryType") ? int.Parse(contractData.GetValue("recoveryType")) : 0;
                if (type != 1) { return; }

                string partName = contractData.GetValue("partName");
                if (!string.IsNullOrEmpty(partName) && !isValidRecoveryPod(partName))
                {
                    string newPart = getRandomRecoveyPod();
                    MonoBehaviour.print("Rescue Pod Fix - Invalid rescue pod detected: " + partName + ", replaced with: " + newPart);
                    contractData.SetValue("partName", newPart, true);
                    Contract.Load(contract, contractData);
                }
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
        }

        private bool isValidRecoveryPod(string name)
        {
            //quick and dirty array.contains test for the input name
            return Array.Exists(approvedPodTypes, m => m == name);
        }

        private string getRandomRecoveyPod()
        {
            if (approvedPodTypes.Length <= 0) { return "landerCabinSmall"; }
            int chosen = rng.Next(approvedPodTypes.Length);
            return approvedPodTypes[chosen];
        }

    }
}
