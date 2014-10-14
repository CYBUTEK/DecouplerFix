// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

#region Using Directives

using System;

using UnityEngine;

#endregion

namespace DecouplerFix
{
    public class ModuleAnchoredDecouplerFix : PartModule, IModuleInfo
    {
        #region Fields

        [KSPField]
        public string anchorName = "anchor";

        [KSPField]
        public float ejectionForce = 50.0f;

        [KSPField]
        public string explosiveNodeID = "srf";

        [KSPField(isPersistant = true)]
        public bool isDecoupled;

        [KSPField]
        public float massLimiterMaximum = 10.0f;

        [KSPField]
        public float massLimiterMinimum = 0.0f;

        [KSPField(isPersistant = true, guiName = "N/kg", guiActiveEditor = true), UI_FloatRange(minValue = 0.0f, maxValue = 10.0f, stepIncrement = 0.1f)]
        public float newtonsPerKilogram = 5.0f;

        #endregion

        #region Methods: public

        public void Decouple()
        {
            AttachNode explosiveAttachNode;
            if (this.isDecoupled || !this.part.isControllable || !this.GetExplosiveAttachNode(out explosiveAttachNode))
            {
                return;
            }

            this.PlayFx();
            this.SplitDecoupler(explosiveAttachNode);

            var debrisPart = explosiveAttachNode.attachedPart == this.part.parent ? this.part : explosiveAttachNode.attachedPart;
            var vesselPart = debrisPart.parent;
            debrisPart.decouple();

            float force;
            var mass = 0.0f;
            this.Eject(debrisPart, vesselPart, out force, ref mass);

            this.isDecoupled = true;
            Debug.Log("[Staging] " + debrisPart.name + " decoupled " + mass.ToString("N1") + "kg with " + force.ToString("N2") + "kN of force.");
        }

        [KSPAction("Decouple")]
        public void DecoupleAction(KSPActionParam action)
        {
            this.Decouple();
        }

        [KSPEvent(guiName = "Decouple", guiActive = true)]
        public void DecoupleEvent()
        {
            this.Decouple();
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public override string GetInfo()
        {
            return "Ejection Force: " + this.ejectionForce.ToString("N1") + Environment.NewLine +
                   "Mass Limiter: " + this.massLimiterMinimum + " to " + this.massLimiterMaximum + " N/kg";
        }

        public string GetModuleTitle()
        {
            return "Decoupler (Fixed)";
        }

        public string GetPrimaryField()
        {
            return "<b>Ejection Force:</b> " + this.ejectionForce.ToString("N1") + Environment.NewLine +
                   "<b>Mass Limited</b>";
        }

        public override void OnActive()
        {
            this.Decouple();
        }

        #endregion

        #region Methods: private

        private void Eject(Part debrisPart, Part vesselPart, out float force, ref float mass)
        {
            this.GetTreeMass(debrisPart, ref mass);

            force = Mathf.Clamp((mass * this.newtonsPerKilogram) * 0.001f, 0.0f, this.ejectionForce);
            debrisPart.rigidbody.AddRelativeForce(Vector3.left * force * 0.5f, ForceMode.Impulse);
            vesselPart.rigidbody.AddForce(debrisPart.transform.right * force * 0.5f, ForceMode.Impulse);
        }

        private bool GetExplosiveAttachNode(out AttachNode explosiveAttachNode)
        {
            explosiveAttachNode = this.explosiveNodeID == "srf" ? this.part.srfAttachNode : this.part.findAttachNode(this.explosiveNodeID);

            return explosiveAttachNode != null && explosiveAttachNode.attachedPart != null;
        }

        private void GetTreeMass(Part root, ref float mass)
        {
            mass += (root.mass + root.GetResourceMass()) * 1000.0f;

            foreach (var child in root.children)
            {
                this.GetTreeMass(child, ref mass);
            }
        }

        private void PlayFx()
        {
            var fxGroup = this.part.findFxGroup("decouple");
            if (fxGroup != null)
            {
                fxGroup.Burst();
            }
        }

        private void SplitDecoupler(AttachNode attachNode)
        {
            var anchorTransform = this.part.FindModelTransform(this.anchorName);
            if (anchorTransform != null)
            {
                anchorTransform.parent = attachNode.attachedPart.transform;
            }
        }

        #endregion
    }
}