﻿/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;

namespace OpenSim.Region.Environment.Scenes
{
    public class SceneExternalChecks
    {
        private Scene m_scene;

        public SceneExternalChecks(Scene scene)
        {
            m_scene = scene;
        }

        #region Object Permission Checks

            public delegate uint GenerateClientFlags(LLUUID userID, LLUUID objectIDID);
            private List<GenerateClientFlags> GenerateClientFlagsCheckFunctions = new List<GenerateClientFlags>();

            public void addGenerateClientFlags(GenerateClientFlags delegateFunc)
            {
                if (!GenerateClientFlagsCheckFunctions.Contains(delegateFunc))
                    GenerateClientFlagsCheckFunctions.Add(delegateFunc);
            }
            public void removeGenerateClientFlags(GenerateClientFlags delegateFunc)
            {
                if (GenerateClientFlagsCheckFunctions.Contains(delegateFunc))
                    GenerateClientFlagsCheckFunctions.Remove(delegateFunc);
            }

            public uint ExternalChecksGenerateClientFlags(LLUUID userID, LLUUID objectID)
            {
                SceneObjectPart part=m_scene.GetSceneObjectPart(objectID);
                
                if (part == null)
                    return 0;

                uint perms=part.GetEffectiveObjectFlags() |
                        (uint)LLObject.ObjectFlags.ObjectModify |
                        (uint)LLObject.ObjectFlags.ObjectCopy |
                        (uint)LLObject.ObjectFlags.ObjectMove |
                        (uint)LLObject.ObjectFlags.ObjectTransfer |
                        (uint)LLObject.ObjectFlags.ObjectYouOwner |
                        (uint)LLObject.ObjectFlags.ObjectAnyOwner |
                        (uint)LLObject.ObjectFlags.ObjectOwnerModify |
                        (uint)LLObject.ObjectFlags.ObjectYouOfficer;

                foreach (GenerateClientFlags check in GenerateClientFlagsCheckFunctions)
                {
                    perms &= check(userID, objectID);
                }
                return perms;
            }

            public delegate void SetBypassPermissions(bool value);
            private List<SetBypassPermissions> SetBypassPermissionsCheckFunctions = new List<SetBypassPermissions>();

            public void addSetBypassPermissions(SetBypassPermissions delegateFunc)
            {
                if (!SetBypassPermissionsCheckFunctions.Contains(delegateFunc))
                    SetBypassPermissionsCheckFunctions.Add(delegateFunc);
            }
            public void removeSetBypassPermissions(SetBypassPermissions delegateFunc)
            {
                if (SetBypassPermissionsCheckFunctions.Contains(delegateFunc))
                    SetBypassPermissionsCheckFunctions.Remove(delegateFunc);
            }

            public void ExternalChecksSetBypassPermissions(bool value)
            {
                foreach (SetBypassPermissions check in SetBypassPermissionsCheckFunctions)
                {
                    check(value);
                }
            }

            public delegate bool BypassPermissions();
            private List<BypassPermissions> BypassPermissionsCheckFunctions = new List<BypassPermissions>();

            public void addBypassPermissions(BypassPermissions delegateFunc)
            {
                if (!BypassPermissionsCheckFunctions.Contains(delegateFunc))
                    BypassPermissionsCheckFunctions.Add(delegateFunc);
            }
            public void removeBypassPermissions(BypassPermissions delegateFunc)
            {
                if (BypassPermissionsCheckFunctions.Contains(delegateFunc))
                    BypassPermissionsCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksBypassPermissions()
            {
                foreach (BypassPermissions check in BypassPermissionsCheckFunctions)
                {
                    if (check() == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool PropagatePermissions();
            private List<PropagatePermissions> PropagatePermissionsCheckFunctions = new List<PropagatePermissions>();

            public void addPropagatePermissions(PropagatePermissions delegateFunc)
            {
                if (!PropagatePermissionsCheckFunctions.Contains(delegateFunc))
                    PropagatePermissionsCheckFunctions.Add(delegateFunc);
            }
            public void removePropagatePermissions(PropagatePermissions delegateFunc)
            {
                if (PropagatePermissionsCheckFunctions.Contains(delegateFunc))
                    PropagatePermissionsCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksPropagatePermissions()
            {
                foreach (PropagatePermissions check in PropagatePermissionsCheckFunctions)
                {
                    if (check() == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #region REZ OBJECT
            public delegate bool CanRezObject(int objectCount, LLUUID owner, LLVector3 objectPosition, Scene scene);
            private List<CanRezObject> CanRezObjectCheckFunctions = new List<CanRezObject>();

            public void addCheckRezObject(CanRezObject delegateFunc)
            {
                if (!CanRezObjectCheckFunctions.Contains(delegateFunc))
                    CanRezObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckRezObject(CanRezObject delegateFunc)
            {
                if (CanRezObjectCheckFunctions.Contains(delegateFunc))
                    CanRezObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanRezObject(int objectCount, LLUUID owner, LLVector3 objectPosition)
            {
                foreach (CanRezObject check in CanRezObjectCheckFunctions)
                {
                    if (check(objectCount, owner,objectPosition, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region DELETE OBJECT
            public delegate bool CanDeleteObject(LLUUID objectID, LLUUID deleter, Scene scene);
            private List<CanDeleteObject> CanDeleteObjectCheckFunctions = new List<CanDeleteObject>();

            public void addCheckDeleteObject(CanDeleteObject delegateFunc)
            {
                if (!CanDeleteObjectCheckFunctions.Contains(delegateFunc))
                    CanDeleteObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckDeleteObject(CanDeleteObject delegateFunc)
            {
                if (CanDeleteObjectCheckFunctions.Contains(delegateFunc))
                    CanDeleteObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanDeleteObject(LLUUID objectID, LLUUID deleter)
            {
                foreach (CanDeleteObject check in CanDeleteObjectCheckFunctions)
                {
                    if (check(objectID,deleter,m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region TAKE OBJECT
            public delegate bool CanTakeObject(LLUUID objectID, LLUUID stealer, Scene scene);
            private List<CanTakeObject> CanTakeObjectCheckFunctions = new List<CanTakeObject>();

            public void addCheckTakeObject(CanTakeObject delegateFunc)
            {
                if (!CanTakeObjectCheckFunctions.Contains(delegateFunc))
                    CanTakeObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckTakeObject(CanTakeObject delegateFunc)
            {
                if (CanTakeObjectCheckFunctions.Contains(delegateFunc))
                    CanTakeObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanTakeObject(LLUUID objectID, LLUUID AvatarTakingUUID)
            {
                foreach (CanTakeObject check in CanTakeObjectCheckFunctions)
                {
                    if (check(objectID, AvatarTakingUUID, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region TAKE COPY OBJECT
            public delegate bool CanTakeCopyObject(LLUUID objectID, LLUUID userID, Scene inScene);
            private List<CanTakeCopyObject> CanTakeCopyObjectCheckFunctions = new List<CanTakeCopyObject>();

            public void addCheckTakeCopyObject(CanTakeCopyObject delegateFunc)
            {
                if (!CanTakeCopyObjectCheckFunctions.Contains(delegateFunc))
                    CanTakeCopyObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckTakeCopyObject(CanTakeCopyObject delegateFunc)
            {
                if (CanTakeCopyObjectCheckFunctions.Contains(delegateFunc))
                    CanTakeCopyObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanTakeCopyObject(LLUUID objectID, LLUUID userID)
            {
                foreach (CanTakeCopyObject check in CanTakeCopyObjectCheckFunctions)
                {
                    if (check(objectID,userID,m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region DUPLICATE OBJECT
            public delegate bool CanDuplicateObject(int objectCount, LLUUID objectID, LLUUID owner, Scene scene, LLVector3 objectPosition);
            private List<CanDuplicateObject> CanDuplicateObjectCheckFunctions = new List<CanDuplicateObject>();

            public void addCheckDuplicateObject(CanDuplicateObject delegateFunc)
            {
                if (!CanDuplicateObjectCheckFunctions.Contains(delegateFunc))
                    CanDuplicateObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckDuplicateObject(CanDuplicateObject delegateFunc)
            {
                if (CanDuplicateObjectCheckFunctions.Contains(delegateFunc))
                    CanDuplicateObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanDuplicateObject(int objectCount, LLUUID objectID, LLUUID owner, LLVector3 objectPosition)
            {
                foreach (CanDuplicateObject check in CanDuplicateObjectCheckFunctions)
                {
                    if (check(objectCount, objectID, owner, m_scene, objectPosition) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region EDIT OBJECT
            public delegate bool CanEditObject(LLUUID objectID, LLUUID editorID, Scene scene);
            private List<CanEditObject> CanEditObjectCheckFunctions = new List<CanEditObject>();

            public void addCheckEditObject(CanEditObject delegateFunc)
            {
                if (!CanEditObjectCheckFunctions.Contains(delegateFunc))
                    CanEditObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckEditObject(CanEditObject delegateFunc)
            {
                if (CanEditObjectCheckFunctions.Contains(delegateFunc))
                    CanEditObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanEditObject(LLUUID objectID, LLUUID editorID)
            {
                foreach (CanEditObject check in CanEditObjectCheckFunctions)
                {
                    if (check(objectID, editorID, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region MOVE OBJECT
            public delegate bool CanMoveObject(LLUUID objectID, LLUUID moverID, Scene scene);
            private List<CanMoveObject> CanMoveObjectCheckFunctions = new List<CanMoveObject>();

            public void addCheckMoveObject(CanMoveObject delegateFunc)
            {
                if (!CanMoveObjectCheckFunctions.Contains(delegateFunc))
                    CanMoveObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckMoveObject(CanMoveObject delegateFunc)
            {
                if (CanMoveObjectCheckFunctions.Contains(delegateFunc))
                    CanMoveObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanMoveObject(LLUUID objectID, LLUUID moverID)
            {
                foreach (CanMoveObject check in CanMoveObjectCheckFunctions)
                {
                    if (check(objectID,moverID,m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region OBJECT ENTRY
            public delegate bool CanObjectEntry(LLUUID objectID, LLVector3 newPoint, Scene scene);
            private List<CanObjectEntry> CanObjectEntryCheckFunctions = new List<CanObjectEntry>();

            public void addCheckObjectEntry(CanObjectEntry delegateFunc)
            {
                if (!CanObjectEntryCheckFunctions.Contains(delegateFunc))
                    CanObjectEntryCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckObjectEntry(CanObjectEntry delegateFunc)
            {
                if (CanObjectEntryCheckFunctions.Contains(delegateFunc))
                    CanObjectEntryCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanObjectEntry(LLUUID objectID, LLVector3 newPoint)
            {
                foreach (CanObjectEntry check in CanObjectEntryCheckFunctions)
                {
                    if (check(objectID, newPoint, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region RETURN OBJECT
            public delegate bool CanReturnObject(LLUUID objectID, LLUUID returnerID, Scene scene);
            private List<CanReturnObject> CanReturnObjectCheckFunctions = new List<CanReturnObject>();

            public void addCheckReturnObject(CanReturnObject delegateFunc)
            {
                if (!CanReturnObjectCheckFunctions.Contains(delegateFunc))
                    CanReturnObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckReturnObject(CanReturnObject delegateFunc)
            {
                if (CanReturnObjectCheckFunctions.Contains(delegateFunc))
                    CanReturnObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanReturnObject(LLUUID objectID, LLUUID returnerID)
            {
                foreach (CanReturnObject check in CanReturnObjectCheckFunctions)
                {
                    if (check(objectID,returnerID,m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region INSTANT MESSAGE
            public delegate bool CanInstantMessage(LLUUID user, LLUUID target, Scene startScene);
            private List<CanInstantMessage> CanInstantMessageCheckFunctions = new List<CanInstantMessage>();

            public void addCheckInstantMessage(CanInstantMessage delegateFunc)
            {
                if (!CanInstantMessageCheckFunctions.Contains(delegateFunc))
                    CanInstantMessageCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckInstantMessage(CanInstantMessage delegateFunc)
            {
                if (CanInstantMessageCheckFunctions.Contains(delegateFunc))
                    CanInstantMessageCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanInstantMessage(LLUUID user, LLUUID target)
            {
                foreach (CanInstantMessage check in CanInstantMessageCheckFunctions)
                {
                    if (check(user, target, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region INVENTORY TRANSFER
            public delegate bool CanInventoryTransfer(LLUUID user, LLUUID target, Scene startScene);
            private List<CanInventoryTransfer> CanInventoryTransferCheckFunctions = new List<CanInventoryTransfer>();

            public void addCheckInventoryTransfer(CanInventoryTransfer delegateFunc)
            {
                if (!CanInventoryTransferCheckFunctions.Contains(delegateFunc))
                    CanInventoryTransferCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckInventoryTransfer(CanInventoryTransfer delegateFunc)
            {
                if (CanInventoryTransferCheckFunctions.Contains(delegateFunc))
                    CanInventoryTransferCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanInventoryTransfer(LLUUID user, LLUUID target)
            {
                foreach (CanInventoryTransfer check in CanInventoryTransferCheckFunctions)
                {
                    if (check(user, target, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region VIEW SCRIPT
            public delegate bool CanViewScript(LLUUID script, LLUUID objectID, LLUUID user, Scene scene);
            private List<CanViewScript> CanViewScriptCheckFunctions = new List<CanViewScript>();

            public void addCheckViewScript(CanViewScript delegateFunc)
            {
                if (!CanViewScriptCheckFunctions.Contains(delegateFunc))
                    CanViewScriptCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckViewScript(CanViewScript delegateFunc)
            {
                if (CanViewScriptCheckFunctions.Contains(delegateFunc))
                    CanViewScriptCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanViewScript(LLUUID script, LLUUID objectID, LLUUID user)
            {
                foreach (CanViewScript check in CanViewScriptCheckFunctions)
                {
                    if (check(script, objectID, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool CanViewNotecard(LLUUID script, LLUUID objectID, LLUUID user, Scene scene);
            private List<CanViewNotecard> CanViewNotecardCheckFunctions = new List<CanViewNotecard>();

            public void addCheckViewNotecard(CanViewNotecard delegateFunc)
            {
                if (!CanViewNotecardCheckFunctions.Contains(delegateFunc))
                    CanViewNotecardCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckViewNotecard(CanViewNotecard delegateFunc)
            {
                if (CanViewNotecardCheckFunctions.Contains(delegateFunc))
                    CanViewNotecardCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanViewNotecard(LLUUID script, LLUUID objectID, LLUUID user)
            {
                foreach (CanViewNotecard check in CanViewNotecardCheckFunctions)
                {
                    if (check(script, objectID, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region EDIT SCRIPT
            public delegate bool CanEditScript(LLUUID script, LLUUID objectID, LLUUID user, Scene scene);
            private List<CanEditScript> CanEditScriptCheckFunctions = new List<CanEditScript>();

            public void addCheckEditScript(CanEditScript delegateFunc)
            {
                if (!CanEditScriptCheckFunctions.Contains(delegateFunc))
                    CanEditScriptCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckEditScript(CanEditScript delegateFunc)
            {
                if (CanEditScriptCheckFunctions.Contains(delegateFunc))
                    CanEditScriptCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanEditScript(LLUUID script, LLUUID objectID, LLUUID user)
            {
                foreach (CanEditScript check in CanEditScriptCheckFunctions)
                {
                    if (check(script, objectID, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool CanEditNotecard(LLUUID notecard, LLUUID objectID, LLUUID user, Scene scene);
            private List<CanEditNotecard> CanEditNotecardCheckFunctions = new List<CanEditNotecard>();

            public void addCheckEditNotecard(CanEditNotecard delegateFunc)
            {
                if (!CanEditNotecardCheckFunctions.Contains(delegateFunc))
                    CanEditNotecardCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckEditNotecard(CanEditNotecard delegateFunc)
            {
                if (CanEditNotecardCheckFunctions.Contains(delegateFunc))
                    CanEditNotecardCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanEditNotecard(LLUUID script, LLUUID objectID, LLUUID user)
            {
                foreach (CanEditNotecard check in CanEditNotecardCheckFunctions)
                {
                    if (check(script, objectID, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region RUN SCRIPT (When Script Placed in Object)
            public delegate bool CanRunScript(LLUUID script, LLUUID objectID, LLUUID user, Scene scene);
            private List<CanRunScript> CanRunScriptCheckFunctions = new List<CanRunScript>();

            public void addCheckRunScript(CanRunScript delegateFunc)
            {
                if (!CanRunScriptCheckFunctions.Contains(delegateFunc))
                    CanRunScriptCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckRunScript(CanRunScript delegateFunc)
            {
                if (CanRunScriptCheckFunctions.Contains(delegateFunc))
                    CanRunScriptCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanRunScript(LLUUID script, LLUUID objectID, LLUUID user)
            {
                foreach (CanRunScript check in CanRunScriptCheckFunctions)
                {
                    if (check(script, objectID, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region START SCRIPT (When Script run box is Checked after placed in object)
            public delegate bool CanStartScript(LLUUID script, LLUUID user, Scene scene);
            private List<CanStartScript> CanStartScriptCheckFunctions = new List<CanStartScript>();

            public void addCheckStartScript(CanStartScript delegateFunc)
            {
                if (!CanStartScriptCheckFunctions.Contains(delegateFunc))
                    CanStartScriptCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckStartScript(CanStartScript delegateFunc)
            {
                if (CanStartScriptCheckFunctions.Contains(delegateFunc))
                    CanStartScriptCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanStartScript(LLUUID script, LLUUID user)
            {
                foreach (CanStartScript check in CanStartScriptCheckFunctions)
                {
                    if (check(script, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

        #endregion

            #region STOP SCRIPT (When Script run box is unchecked after placed in object)
            public delegate bool CanStopScript(LLUUID script, LLUUID user, Scene scene);
            private List<CanStopScript> CanStopScriptCheckFunctions = new List<CanStopScript>();

            public void addCheckStopScript(CanStopScript delegateFunc)
            {
                if (!CanStopScriptCheckFunctions.Contains(delegateFunc))
                    CanStopScriptCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckStopScript(CanStopScript delegateFunc)
            {
                if (CanStopScriptCheckFunctions.Contains(delegateFunc))
                    CanStopScriptCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanStopScript(LLUUID script, LLUUID user)
            {
                foreach (CanStopScript check in CanStopScriptCheckFunctions)
                {
                    if (check(script, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region RESET SCRIPT
            public delegate bool CanResetScript(LLUUID script, LLUUID user, Scene scene);
            private List<CanResetScript> CanResetScriptCheckFunctions = new List<CanResetScript>();

            public void addCheckResetScript(CanResetScript delegateFunc)
            {
                if (!CanResetScriptCheckFunctions.Contains(delegateFunc))
                    CanResetScriptCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckResetScript(CanResetScript delegateFunc)
            {
                if (CanResetScriptCheckFunctions.Contains(delegateFunc))
                    CanResetScriptCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanResetScript(LLUUID script, LLUUID user)
            {
                foreach (CanResetScript check in CanResetScriptCheckFunctions)
                {
                    if (check(script, user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region TERRAFORM LAND
            public delegate bool CanTerraformLand(LLUUID user, LLVector3 position, Scene requestFromScene);
            private List<CanTerraformLand> CanTerraformLandCheckFunctions = new List<CanTerraformLand>();

            public void addCheckTerraformLand(CanTerraformLand delegateFunc)
            {
                if (!CanTerraformLandCheckFunctions.Contains(delegateFunc))
                    CanTerraformLandCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckTerraformLand(CanTerraformLand delegateFunc)
            {
                if (CanTerraformLandCheckFunctions.Contains(delegateFunc))
                    CanTerraformLandCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanTerraformLand(LLUUID user, LLVector3 pos)
            {
                foreach (CanTerraformLand check in CanTerraformLandCheckFunctions)
                {
                    if (check(user, pos, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region RUN CONSOLE COMMAND
            public delegate bool CanRunConsoleCommand(LLUUID user, Scene requestFromScene);
            private List<CanRunConsoleCommand> CanRunConsoleCommandCheckFunctions = new List<CanRunConsoleCommand>();

            public void addCheckRunConsoleCommand(CanRunConsoleCommand delegateFunc)
            {
                if (!CanRunConsoleCommandCheckFunctions.Contains(delegateFunc))
                    CanRunConsoleCommandCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckRunConsoleCommand(CanRunConsoleCommand delegateFunc)
            {
                if (CanRunConsoleCommandCheckFunctions.Contains(delegateFunc))
                    CanRunConsoleCommandCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanRunConsoleCommand(LLUUID user)
            {
                foreach (CanRunConsoleCommand check in CanRunConsoleCommandCheckFunctions)
                {
                    if (check(user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            #endregion

            #region CAN ISSUE ESTATE COMMAND
            public delegate bool CanIssueEstateCommand(LLUUID user, Scene requestFromScene);
            private List<CanIssueEstateCommand> CanIssueEstateCommandCheckFunctions = new List<CanIssueEstateCommand>();

            public void addCheckIssueEstateCommand(CanIssueEstateCommand delegateFunc)
            {
                if (!CanIssueEstateCommandCheckFunctions.Contains(delegateFunc))
                    CanIssueEstateCommandCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckIssueEstateCommand(CanIssueEstateCommand delegateFunc)
            {
                if (CanIssueEstateCommandCheckFunctions.Contains(delegateFunc))
                    CanIssueEstateCommandCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanIssueEstateCommand(LLUUID user)
            {
                foreach (CanIssueEstateCommand check in CanIssueEstateCommandCheckFunctions)
                {
                    if (check(user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            #endregion

            #region CAN BE GODLIKE
            public delegate bool CanBeGodLike(LLUUID user, Scene requestFromScene);
            private List<CanBeGodLike> CanBeGodLikeCheckFunctions = new List<CanBeGodLike>();

            public void addCheckBeGodLike(CanBeGodLike delegateFunc)
            {
                if (!CanBeGodLikeCheckFunctions.Contains(delegateFunc))
                    CanBeGodLikeCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckBeGodLike(CanBeGodLike delegateFunc)
            {
                if (CanBeGodLikeCheckFunctions.Contains(delegateFunc))
                    CanBeGodLikeCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanBeGodLike(LLUUID user)
            {
                foreach (CanBeGodLike check in CanBeGodLikeCheckFunctions)
                {
                    if (check(user, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            #endregion

            #region EDIT PARCEL
            public delegate bool CanEditParcel(LLUUID user, ILandObject parcel, Scene scene);
            private List<CanEditParcel> CanEditParcelCheckFunctions = new List<CanEditParcel>();

            public void addCheckEditParcel(CanEditParcel delegateFunc)
            {
                if (!CanEditParcelCheckFunctions.Contains(delegateFunc))
                    CanEditParcelCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckEditParcel(CanEditParcel delegateFunc)
            {
                if (CanEditParcelCheckFunctions.Contains(delegateFunc))
                    CanEditParcelCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanEditParcel(LLUUID user, ILandObject parcel)
            {
                foreach (CanEditParcel check in CanEditParcelCheckFunctions)
                {
                    if (check(user, parcel, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            #endregion

            #region SELL PARCEL
            public delegate bool CanSellParcel(LLUUID user, ILandObject parcel, Scene scene);
            private List<CanSellParcel> CanSellParcelCheckFunctions = new List<CanSellParcel>();

            public void addCheckSellParcel(CanSellParcel delegateFunc)
            {
                if (!CanSellParcelCheckFunctions.Contains(delegateFunc))
                    CanSellParcelCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckSellParcel(CanSellParcel delegateFunc)
            {
                if (CanSellParcelCheckFunctions.Contains(delegateFunc))
                    CanSellParcelCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanSellParcel(LLUUID user, ILandObject parcel)
            {
                foreach (CanSellParcel check in CanSellParcelCheckFunctions)
                {
                    if (check(user, parcel, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            #endregion

            #region ABANDON PARCEL
            public delegate bool CanAbandonParcel(LLUUID user, ILandObject parcel, Scene scene);
            private List<CanAbandonParcel> CanAbandonParcelCheckFunctions = new List<CanAbandonParcel>();

            public void addCheckAbandonParcel(CanAbandonParcel delegateFunc)
            {
                if (!CanAbandonParcelCheckFunctions.Contains(delegateFunc))
                    CanAbandonParcelCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckAbandonParcel(CanAbandonParcel delegateFunc)
            {
                if (CanAbandonParcelCheckFunctions.Contains(delegateFunc))
                    CanAbandonParcelCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanAbandonParcel(LLUUID user, ILandObject parcel)
            {
                foreach (CanAbandonParcel check in CanAbandonParcelCheckFunctions)
                {
                    if (check(user, parcel, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            #endregion

            public delegate bool CanReclaimParcel(LLUUID user, ILandObject parcel, Scene scene);
            private List<CanReclaimParcel> CanReclaimParcelCheckFunctions = new List<CanReclaimParcel>();

            public void addCheckReclaimParcel(CanReclaimParcel delegateFunc)
            {
                if (!CanReclaimParcelCheckFunctions.Contains(delegateFunc))
                    CanReclaimParcelCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckReclaimParcel(CanReclaimParcel delegateFunc)
            {
                if (CanReclaimParcelCheckFunctions.Contains(delegateFunc))
                    CanReclaimParcelCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanReclaimParcel(LLUUID user, ILandObject parcel)
            {
                foreach (CanReclaimParcel check in CanReclaimParcelCheckFunctions)
                {
                    if (check(user, parcel, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
            public delegate bool CanBuyLand(LLUUID user, ILandObject parcel, Scene scene);
            private List<CanBuyLand> CanBuyLandCheckFunctions = new List<CanBuyLand>();

            public void addCheckCanBuyLand(CanBuyLand delegateFunc)
            {
                if (!CanBuyLandCheckFunctions.Contains(delegateFunc))
                    CanBuyLandCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckCanBuyLand(CanBuyLand delegateFunc)
            {
                if (CanBuyLandCheckFunctions.Contains(delegateFunc))
                    CanBuyLandCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanBuyLand(LLUUID user, ILandObject parcel)
            {
                foreach (CanBuyLand check in CanBuyLandCheckFunctions)
                {
                    if (check(user, parcel, m_scene) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool CanLinkObject(LLUUID user, LLUUID objectID);
            private List<CanLinkObject> CanLinkObjectCheckFunctions = new List<CanLinkObject>();

            public void addCheckCanLinkObject(CanLinkObject delegateFunc)
            {
                if (!CanLinkObjectCheckFunctions.Contains(delegateFunc))
                    CanLinkObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckCanLinkObject(CanLinkObject delegateFunc)
            {
                if (CanLinkObjectCheckFunctions.Contains(delegateFunc))
                    CanLinkObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanLinkObject(LLUUID user, LLUUID objectID)
            {
                foreach (CanLinkObject check in CanLinkObjectCheckFunctions)
                {
                    if (check(user, objectID) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool CanDelinkObject(LLUUID user, LLUUID objectID);
            private List<CanDelinkObject> CanDelinkObjectCheckFunctions = new List<CanDelinkObject>();

            public void addCheckCanDelinkObject(CanDelinkObject delegateFunc)
            {
                if (!CanDelinkObjectCheckFunctions.Contains(delegateFunc))
                    CanDelinkObjectCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckCanDelinkObject(CanDelinkObject delegateFunc)
            {
                if (CanDelinkObjectCheckFunctions.Contains(delegateFunc))
                    CanDelinkObjectCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanDelinkObject(LLUUID user, LLUUID objectID)
            {
                foreach (CanDelinkObject check in CanDelinkObjectCheckFunctions)
                {
                    if (check(user, objectID) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

        #endregion

            public delegate bool CanCreateInventory(uint invType, LLUUID objectID, LLUUID userID);
            private List<CanCreateInventory> CanCreateInventoryCheckFunctions = new List<CanCreateInventory>();

            public void addCheckCanCreateInventory(CanCreateInventory delegateFunc)
            {
                if (!CanCreateInventoryCheckFunctions.Contains(delegateFunc))
                    CanCreateInventoryCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckCanCreateInventory(CanCreateInventory delegateFunc)
            {
                if (CanCreateInventoryCheckFunctions.Contains(delegateFunc))
                    CanCreateInventoryCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanCreateInventory(uint invType, LLUUID objectID, LLUUID userID)
            {
                foreach (CanCreateInventory check in CanCreateInventoryCheckFunctions)
                {
                    if (check(invType, objectID, userID) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool CanCopyInventory(LLUUID itemID, LLUUID objectID, LLUUID userID);
            private List<CanCopyInventory> CanCopyInventoryCheckFunctions = new List<CanCopyInventory>();

            public void addCheckCanCopyInventory(CanCopyInventory delegateFunc)
            {
                if (!CanCopyInventoryCheckFunctions.Contains(delegateFunc))
                    CanCopyInventoryCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckCanCopyInventory(CanCopyInventory delegateFunc)
            {
                if (CanCopyInventoryCheckFunctions.Contains(delegateFunc))
                    CanCopyInventoryCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanCopyInventory(LLUUID itemID, LLUUID objectID, LLUUID userID)
            {
                foreach (CanCopyInventory check in CanCopyInventoryCheckFunctions)
                {
                    if (check(itemID, objectID, userID) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool CanDeleteInventory(LLUUID itemID, LLUUID objectID, LLUUID userID);
            private List<CanDeleteInventory> CanDeleteInventoryCheckFunctions = new List<CanDeleteInventory>();

            public void addCheckCanDeleteInventory(CanDeleteInventory delegateFunc)
            {
                if (!CanDeleteInventoryCheckFunctions.Contains(delegateFunc))
                    CanDeleteInventoryCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckCanDeleteInventory(CanDeleteInventory delegateFunc)
            {
                if (CanDeleteInventoryCheckFunctions.Contains(delegateFunc))
                    CanDeleteInventoryCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanDeleteInventory(LLUUID itemID, LLUUID objectID, LLUUID userID)
            {
                foreach (CanDeleteInventory check in CanDeleteInventoryCheckFunctions)
                {
                    if (check(itemID, objectID, userID) == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public delegate bool CanTeleport(LLUUID userID);
            private List<CanTeleport> CanTeleportCheckFunctions = new List<CanTeleport>();

            public void addCheckCanTeleport(CanTeleport delegateFunc)
            {
                if (!CanTeleportCheckFunctions.Contains(delegateFunc))
                    CanTeleportCheckFunctions.Add(delegateFunc);
            }
            public void removeCheckCanTeleport(CanTeleport delegateFunc)
            {
                if (CanTeleportCheckFunctions.Contains(delegateFunc))
                    CanTeleportCheckFunctions.Remove(delegateFunc);
            }

            public bool ExternalChecksCanTeleport(LLUUID userID)
            {
                foreach (CanTeleport check in CanTeleportCheckFunctions)
                {
                    if (check(userID) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
    }
}
    
