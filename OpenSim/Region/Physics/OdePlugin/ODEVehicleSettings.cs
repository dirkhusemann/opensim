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
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using OpenMetaverse;
using Ode.NET;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Physics.OdePlugin
{
    public class ODEVehicleSettings
    {
        public Vehicle Type
        {
            get { return m_type; }
        }

        public IntPtr Body
        {
            get { return m_body; }
        }

        private Vehicle m_type = Vehicle.TYPE_NONE;
        private OdeScene m_parentScene = null;
        private IntPtr m_body = IntPtr.Zero;
        private IntPtr m_jointGroup = IntPtr.Zero;
        private IntPtr m_aMotor = IntPtr.Zero;
        private IntPtr m_lMotor = IntPtr.Zero;

        // Vehicle properties
        private Quaternion m_referenceFrame = Quaternion.Identity;
        private Vector3 m_angularFrictionTimescale = Vector3.Zero;
        private Vector3 m_angularMotorDirection = Vector3.Zero;
        private Vector3 m_linearFrictionTimescale = Vector3.Zero;
        private Vector3 m_linearMotorDirection = Vector3.Zero;
        private Vector3 m_linearMotorOffset = Vector3.Zero;
        private float m_angularDeflectionEfficiency = 0;
        private float m_angularDeflectionTimescale = 0;
        private float m_angularMotorDecayTimescale = 0;
        private float m_angularMotorTimescale = 0;
        private float m_bankingEfficiency = 0;
        private float m_bankingMix = 0;
        private float m_bankingTimescale = 0;
        private float m_buoyancy = 0;
        private float m_hoverHeight = 0;
        private float m_hoverEfficiency = 0;
        private float m_hoverTimescale = 0;
        private float m_linearDeflectionEfficiency = 0;
        private float m_linearDeflectionTimescale = 0;
        private float m_linearMotorDecayTimescale = 0;
        private float m_linearMotorTimescale = 0;
        private float m_verticalAttractionEfficiency = 0;
        private float m_verticalAttractionTimescale = 0;
        private Vector3 m_lastVector = Vector3.Zero;
        private VehicleFlag m_flags = (VehicleFlag) 0;




        internal void ProcessFloatVehicleParam(Vehicle pParam, float pValue)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_DEFLECTION_EFFICIENCY:
                    m_angularDeflectionEfficiency = pValue;
                    break;
                case Vehicle.ANGULAR_DEFLECTION_TIMESCALE:
                    m_angularDeflectionTimescale = pValue;
                    break;
                case Vehicle.ANGULAR_MOTOR_DECAY_TIMESCALE:
                    m_angularMotorDecayTimescale = pValue;
                    break;
                case Vehicle.ANGULAR_MOTOR_TIMESCALE:
                    m_angularMotorTimescale = pValue;
                    break;
                case Vehicle.BANKING_EFFICIENCY:
                    m_bankingEfficiency = pValue;
                    break;
                case Vehicle.BANKING_MIX:
                    m_bankingMix = pValue;
                    break;
                case Vehicle.BANKING_TIMESCALE:
                    m_bankingTimescale = pValue;
                    break;
                case Vehicle.BUOYANCY:
                    m_buoyancy = pValue;
                    break;
                case Vehicle.HOVER_EFFICIENCY:
                    m_hoverEfficiency = pValue;
                    break;
                case Vehicle.HOVER_HEIGHT:
                    m_hoverHeight = pValue;
                    break;
                case Vehicle.HOVER_TIMESCALE:
                    m_hoverTimescale = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_EFFICIENCY:
                    m_linearDeflectionEfficiency = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_TIMESCALE:
                    m_linearDeflectionTimescale = pValue;
                    break;
                case Vehicle.LINEAR_MOTOR_DECAY_TIMESCALE:
                    m_linearMotorDecayTimescale = pValue;
                    break;
                case Vehicle.LINEAR_MOTOR_TIMESCALE:
                    m_linearMotorTimescale = pValue;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_EFFICIENCY:
                    m_verticalAttractionEfficiency = pValue;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_TIMESCALE:
                    m_verticalAttractionTimescale = pValue;
                    break;
                    
                // These are vector properties but the engine lets you use a single float value to 
                // set all of the components to the same value
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    m_angularFrictionTimescale = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    m_linearFrictionTimescale = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.LINEAR_MOTOR_DIRECTION:
                    m_linearMotorDirection = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.LINEAR_MOTOR_OFFSET:
                    m_linearMotorOffset = new Vector3(pValue, pValue, pValue);
                    break;

            }
            Reset();
        }

        internal void ProcessVectorVehicleParam(Vehicle pParam, PhysicsVector pValue)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    m_angularFrictionTimescale = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    m_linearFrictionTimescale = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.LINEAR_MOTOR_DIRECTION:
                    m_linearMotorDirection = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.LINEAR_MOTOR_OFFSET:
                    m_linearMotorOffset = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
            }
            Reset();
        }

        internal void ProcessRotationVehicleParam(Vehicle pParam, Quaternion pValue)
        {
            switch (pParam)
            {
                case Vehicle.REFERENCE_FRAME:
                    m_referenceFrame = pValue;
                    break;
            }
            Reset();
        }

        internal void ProcessTypeChange(Vehicle pType)
        {
            if (m_type == Vehicle.TYPE_NONE && pType != Vehicle.TYPE_NONE)
            {
                // Activate whatever it is
                SetDefaultsForType(pType);
                Reset();
            }
            else if (m_type != Vehicle.TYPE_NONE && pType != Vehicle.TYPE_NONE )
            {
                // Set properties
                SetDefaultsForType(pType);
                // then reset
                Reset();
            }
            else if (m_type != Vehicle.TYPE_NONE && pType == Vehicle.TYPE_NONE)
            {
                Destroy();
            }
        }

        internal void Disable()
        {
            if (m_body == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            
        }

        internal void Enable(IntPtr pBody, OdeScene pParentScene)
        {
            if (m_type == Vehicle.TYPE_NONE)
                return;

            m_body = pBody;
            m_parentScene = pParentScene;
        }

        internal void Reset()
        {
            if (m_body == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            
        }

        internal void Destroy()
        {
            if (m_body == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            
        }

        internal void Step(float pTimestep)
        {
            if (m_body == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            VerticalAttractor(pTimestep);
            LinearMotor(pTimestep);
        }

        private void SetDefaultsForType(Vehicle pType)
        {
            m_type = pType;
            switch (pType)
            {
                case Vehicle.TYPE_SLED:
                    m_linearFrictionTimescale = new Vector3(30, 1, 1000);
                    m_angularFrictionTimescale = new Vector3(1000, 1000, 1000);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1000;
                    m_linearMotorDecayTimescale = 120;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1000;
                    m_angularMotorDecayTimescale = 120;
                    m_hoverHeight = 0;
                    m_hoverEfficiency = 10;
                    m_hoverTimescale = 10;
                    m_buoyancy = 0;
                    m_linearDeflectionEfficiency = 1;
                    m_linearDeflectionTimescale = 1;
                    m_angularDeflectionEfficiency = 1;
                    m_angularDeflectionTimescale = 1000;
                    m_bankingEfficiency = 0;
                    m_bankingMix = 1;
                    m_bankingTimescale = 10;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &=
                        ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                          VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_CAR:
                    m_linearFrictionTimescale = new Vector3(100, 2, 1000);
                    m_angularFrictionTimescale = new Vector3(1000, 1000, 1000);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1;
                    m_angularMotorDecayTimescale = 0.8f;
                    m_hoverHeight = 0;
                    m_hoverEfficiency = 0;
                    m_hoverTimescale = 1000;
                    m_buoyancy = 0;
                    m_linearDeflectionEfficiency = 1;
                    m_linearDeflectionTimescale = 2;
                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 10;
                    m_verticalAttractionEfficiency = 1;
                    m_verticalAttractionTimescale = 10;
                    m_bankingEfficiency = -0.2f;
                    m_bankingMix = 1;
                    m_bankingTimescale = 1;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.HOVER_UP_ONLY |
                                VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_BOAT:
                    m_linearFrictionTimescale = new Vector3(10, 3, 2);
                    m_angularFrictionTimescale = new Vector3(10,10,10);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4;
                    m_angularMotorDecayTimescale = 4;
                    m_hoverHeight = 0;
                    m_hoverEfficiency = 0.5f;
                    m_hoverTimescale = 2;
                    m_buoyancy = 1;
                    m_linearDeflectionEfficiency = 0.5f;
                    m_linearDeflectionTimescale = 3;
                    m_angularDeflectionEfficiency = 0.5f;
                    m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 0.5f;
                    m_verticalAttractionTimescale = 5;
                    m_bankingEfficiency = -0.3f;
                    m_bankingMix = 0.8f;
                    m_bankingTimescale = 1;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~( VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_UP_ONLY |
                                VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_AIRPLANE:
                    m_linearFrictionTimescale = new Vector3(200, 10, 5);
                    m_angularFrictionTimescale = new Vector3(20, 20, 20);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 2;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4;
                    m_angularMotorDecayTimescale = 4;
                    m_hoverHeight = 0;
                    m_hoverEfficiency = 0.5f;
                    m_hoverTimescale = 1000;
                    m_buoyancy = 0;
                    m_linearDeflectionEfficiency = 0.5f;
                    m_linearDeflectionTimescale = 3;
                    m_angularDeflectionEfficiency = 1;
                    m_angularDeflectionTimescale = 2;
                    m_verticalAttractionEfficiency = 0.9f;
                    m_verticalAttractionTimescale = 2;
                    m_bankingEfficiency = 1;
                    m_bankingMix = 0.7f;
                    m_bankingTimescale = 2;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                        VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY | VehicleFlag.LIMIT_MOTOR_UP);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY);
                    break;
                case Vehicle.TYPE_BALLOON:
                    m_linearFrictionTimescale = new Vector3(5, 5, 5);
                    m_angularFrictionTimescale = new Vector3(10, 10, 10);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 6;
                    m_angularMotorDecayTimescale = 10;
                    m_hoverHeight = 5;
                    m_hoverEfficiency = 0.8f;
                    m_hoverTimescale = 10;
                    m_buoyancy = 1;
                    m_linearDeflectionEfficiency = 0;
                    m_linearDeflectionTimescale = 5;
                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 1;
                    m_verticalAttractionTimescale = 1000;
                    m_bankingEfficiency = 0;
                    m_bankingMix = 0.7f;
                    m_bankingTimescale = 5;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags = (VehicleFlag)0;
                    break;

            }
        }
        
        private void VerticalAttractor(float pTimestep)
        {
            // The purpose of this routine here is to quickly stabilize the Body while it's popped up in the air.
            // The amotor needs a few seconds to stabilize so without it, the avatar shoots up sky high when you
            // change appearance and when you enter the simulator
            // After this routine is done, the amotor stabilizes much quicker
            d.Mass objMass;
            d.BodyGetMass(Body, out objMass);
            //d.BodyGetS

            d.Vector3 feet;
            d.Vector3 head;
            d.BodyGetRelPointPos(m_body, 0.0f, 0.0f, -1.0f, out feet);
            d.BodyGetRelPointPos(m_body, 0.0f, 0.0f, 1.0f, out head);
            float posture = head.Z - feet.Z;
            
            // restoring force proportional to lack of posture:
            float servo = (2.5f - posture) * (objMass.mass * m_verticalAttractionEfficiency / (m_verticalAttractionTimescale * pTimestep)) * objMass.mass;
            d.BodyAddForceAtRelPos(m_body, 0.0f, 0.0f, servo, 0.0f, 0.0f, 1.0f);
            d.BodyAddForceAtRelPos(m_body, 0.0f, 0.0f, -servo, 0.0f, 0.0f, -1.0f);
            //d.Matrix3 bodyrotation = d.BodyGetRotation(Body);
            //m_log.Info("[PHYSICSAV]: Rotation: " + bodyrotation.M00 + " : " + bodyrotation.M01 + " : " + bodyrotation.M02 + " : " + bodyrotation.M10 + " : " + bodyrotation.M11 + " : " + bodyrotation.M12 + " : " + bodyrotation.M20 + " : " + bodyrotation.M21 + " : " + bodyrotation.M22);
        }

        private void LinearMotor(float pTimestep)
        {

            /*
            float decayval = (m_linearMotorDecayTimescale * pTimestep);
            m_linearMotorDirection  *= decayval;
            m_lastVector += m_linearMotorDirection

            
            m_lin
            m_lastVector
             * */
        }
    }
}
