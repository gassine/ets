using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using FivePD.API;
using FivePD.API.Utils;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static TrafficStopPlugin.TrafficStopPlugin;

namespace TrafficStopPlugin
{
    internal class TrafficStopPlugin : Plugin
    {

        // Here we will create a class to track the personality types
        public class PERSONALITY
        {
            // IMPLEMENTATION NOTES:
            // STEP 1 - Add the scenario identifier in this class. Use a number in the same hundreds that has not been used. Example: 211 for evil.
            public class LAWFUL
            {
                public static int STAY = 100;
                public static int EXIT_VEHICLE = 101;
                public static int WALK_TOWARDS_OFFICER = 102;
                public static int WALK_AROUND = 103;
            }

            public class EVIL
            {
                public static int FLEE_THEN_SHOOT = 200;
                public static int SHOOT_WHEN_CLOSE = 201;
                public static int SHOOT_WHEN_OFFICER_IS_OUT = 202;
                public static int SHOOT_AT_RANDOM_TIME = 203;
                public static int ATTACK_WITH_MELEE = 204;
                public static int SHOOT_FROM_START = 205;
                public static int WALK_TOWARDS_SHOOTING = 206;
            }

            public class COWARD
            {
                public static int FLEE_ON_FOOT = 300;
                public static int AIM_SUICIDE = 301;
                public static int VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE = 302;
                public static int VEHICLE_FLEE_AT_RANDOM = 303;
            }

        }

        public class TIME
        {
            public static int SECONDS_1 = 1000;
            public static int SECONDS_5 = 5 * 1000;
            public static int SECONDS_10 = 10 * 1000;
            public static int SECONDS_15 = 15 * 1000;
            public static int SECONDS_20 = 20 * 1000;
            public static int SECONDS_30 = 30 * 1000;
            public static int SECONDS_45 = 45 * 1000;
            public static int MINUTES_1 = 1 * 60 * 1000;
            public static int MINUTES_3 = 3 * 60 * 1000;
            public static int MINUTES_5 = 5 * 60 * 1000;
            public static int MINUTES_10 = 10 * 60 * 1000;
        }
        // END OF PERSONALITY TYPES 

        public class WARRANT_CHANCE
        {
            public static int LAWFUL = 10;
            public static int COWARD = 30;
            public static int EVIL = 50;
        }

        public class ILLEGAL_INVENTORY_ODDS
        {
            public static int LAWFUL = 10;
            public static int COWARD = 50;
            public static int EVIL = 100;
        }

        public class SCENARIO_TYPE
        {
            public static int LAWFUL = 100;
            public static int COWARD = 300;
            public static int EVIL = 200;
        }

        public class ANIMATION_TYPE
        {
            // This is just a recollection of the animation type dictionaries that we might end up using.
            public static string COMPLAIN = "misscommon@response";
            public static string UNHOLSTER_MELEE = "anim@melee@switchblade@holster";
            public static string UNHOLSTER_PISTOL = "weapons@holster_1h";

        }
        public class TRAFFIC_SCENARIO
        {
            public static int SPEEDER = 1;
            public static int BROKEN_CAR = 2;
            public static int AMBER_ALERT = 3;
            public static int CAR_ACCIDENT = 4;
            public static int HEART_ATTACK = 5;
            public static int CAR_FLIPPED = 6;
            public static int BODY_ON_ROAD = 7;
            public static int STOLEN_CAR = 8;
            public static int FLAT_TIRE = 9;
        }

        public class DRIVING_STYLES
        {
            public static int AVOID_PEDS_CARS_OBJECTS = 524732;
            public static int RECKLESS_RUN_LIGHTS = 524604;
            public static int AVOID_VEHICLES = 524556;
            public static int NORMAL = 786603;
            public static int RUSHED = 1074528293;
        }

        // Here we are setting that variable where we will track if the user is on a callout or not.
        private bool enhancedTrafficStop = false;
        Ped tsDriver = null;
        Vehicle tsVehicle = null;
        Ped player = null;
        //int counter = 0; // UNCOMMENT THIS FOR TESTING PURPOSES

        // Now we are starting a ticker. This ticket will check every X amount of time if the player is performing a traffic stop or not.
        internal TrafficStopPlugin()
        {
            // This will triger the enhanced traffic stops
            Tick += CheckForTrafficStop;
            // This will clear the enhanced traffic stop status if player is no longer on a traffic stop.
            Tick += clearEts;
            // EXPERIMENTAL TEST: This will enable the speeders option
            Tick += createSpeeder;
        }


        // In this function we will check if the user is currently on a traffic stop
        public async Task CheckForTrafficStop()
        {
            // TEST BLOCK
            //counter++;
            //Screen.ShowNotification("CHECKING NEW ETS #" + counter);
            //Debug.WriteLine("CHECKING NEW ETS #" + counter);
            // END TEST BLOCK
            
            // This is to avoid ticks getting triggered 1000 times a second.
            await (BaseScript.Delay(5000));

            // Here we are going to check if the player is currently performing a traffic stop.
            if (Utilities.IsPlayerPerformingTrafficStop())
            {
                // In this section we will determien if the player is currently not on an enhanced traffic stop and proceed to make one
                // NOTE: In the future, this will have a randomizer to determine at random if this traffic stop will be enhanced or not.
                if (!enhancedTrafficStop)
                {
                    // First we will set the OfferedCallout to TRUE to avoid repeating the function. This will cancel automatically once the ped has been arrested or killed.
                    enhancedTrafficStop = true;

                    // Now we're going to define who is the driver, and who is the player.
                        
                    tsDriver = Utilities.GetDriverFromTrafficStop();
                    tsVehicle = Utilities.GetVehicleFromTrafficStop();
                    player = Game.PlayerPed;

                    // First we will check that the traffic stop did not turn into a pursuit right away so we can keep those happening as usual
                    if (tsDriver.IsFleeing)
                    {
                        // If it is a vehicle pursuit, we will trigger a return to stop the function from executing.
                        return;
                    }

                    // Now that we will proceed with the Enhanced Traffic Stop we will get the personality that will be sent
                    // Based on the type of personality we will assign a warrant or not
                    // Lawful >= 100 < 200 || Evil >= 200 < 300 || Coward  >= 300
                    int randomPersonality = getRandomPersonality();

                    // Here we will determine what is the scenario type. Lawful, Evil or Coward
                    int scenarioType = 0;
                    if (randomPersonality >= 100 && randomPersonality < 200) { scenarioType = SCENARIO_TYPE.LAWFUL; } // 100-200 are the values for the lawful scenarios
                    else if (randomPersonality >= 200 && randomPersonality < 300) { scenarioType = SCENARIO_TYPE.EVIL; } // 200-300 are the values for the evil scenarios
                    else if (randomPersonality >= 300 && randomPersonality < 400) { scenarioType = SCENARIO_TYPE.LAWFUL; } // 300-400 are the values for the coward scenarios


                    // Now we will also create a variable that will reflect the ODDS based on the type of scenario
                    int randomWarrantChance = 0;
                    // RandomWarrantChance will change how likely it should be for a suspect to have a warrant based on the type of scenario that it will be
                    if (scenarioType == SCENARIO_TYPE.LAWFUL) { randomWarrantChance = WARRANT_CHANCE.LAWFUL; } // Here we are setting the warrant chance that will be added later
                    else if (scenarioType == SCENARIO_TYPE.EVIL) { randomWarrantChance = WARRANT_CHANCE.EVIL; } // Here we are setting the warrant chance that will be added later
                    else if (scenarioType == SCENARIO_TYPE.COWARD) { randomWarrantChance = WARRANT_CHANCE.COWARD; } // Here we are setting the warrant chance that will be added later



                    // Now we will create a randomizer that we will use to compare against the odds of warrant creation
                    int warrantRandomizer = RandomUtils.GetRandomNumber(1, 101);
                    // First we will get the current data of the driver and the car from the traffic stop
                    PedData newPedInfo = await tsDriver.GetData();
                    VehicleData newVehicleData = await tsVehicle.GetData();
                    if (warrantRandomizer <= randomWarrantChance) 
                    {
                        // Now we will change the address inside of his data and assign it a random city
                        newPedInfo.Warrant = getRandomWarrant();
                        // Lastly we update the PED
                        tsDriver.SetData(newPedInfo);

                        // NOTE: This is done here instead of a separate function because of some issues accessing the ped information from a function.
                    }

                    // Lastly we will assign a new type of inventory to the ped and their vehicle based on the type of scenario that's about to happen
                    List<Item> illegalPedItems = new List<Item>();
                    illegalPedItems = getIllegalPedInventory(scenarioType);

                    // Now we will update the ped inventory
                    if (illegalPedItems != null) 
                    {
                        newPedInfo.Items.AddRange(illegalPedItems);
                        tsDriver.SetData(newPedInfo);
                    }
                    /* // NOTE This cannot be done to cars. Tried to create the code but it won't update inventory
                    //List<Item> illegalCarItems = new List<Item>();
                    //illegalCarItems = getIllegalCarInventory(scenarioType); // NOTE This cannot be done to cars. Tried to create the code but it won't update inventory
                    // Now we will update the car inventory
                    if (illegalCarItems != null) 
                    {
                        Screen.ShowNotification("Vehicle Items Not Null: " + illegalCarItems[0].Name);
                        newVehicleData.Items.AddRange(illegalCarItems); 
                        tsVehicle.SetData(newVehicleData);
                        Screen.ShowNotification("VehicleData set");
                    }
                    */

                    //
                    // Now we will trigger the enhanced traffic stop
                    await triggerScenario(randomPersonality, tsDriver, player, tsVehicle);

                    // TEST BLOCK
                    //int scenarioNumber = getRandomPersonality();
                    //Screen.ShowNotification("Initiated enhanced traffic stop on PED ID: " + tsDriver.NetworkId);
                    //Screen.ShowNotification("Scenario Number: " + scenarioNumber);
                    //await triggerScenario(scenarioNumber, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.SHOOT_WHEN_CLOSE, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.ATTACK_WITH_MELEE, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.SHOOT_FROM_START, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.SHOOT_AT_RANDOM_TIME, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.SHOOT_WHEN_OFFICER_IS_OUT, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.FLEE_THEN_SHOOT, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.COWARD.VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.COWARD.FLEE_ON_FOOT, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.COWARD.VEHICLE_FLEE_AT_RANDOM, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.COWARD.AIM_SUICIDE, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.LAWFUL.EXIT_VEHICLE, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.LAWFUL.WALK_TOWARDS_OFFICER, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.LAWFUL.WALK_AROUND, tsDriver, player, tsVehicle);
                    //await triggerScenario(PERSONALITY.EVIL.WALK_TOWARDS_SHOOTING, tsDriver, player, tsVehicle);

                }

            }

            // I don't know what this is for, but seems to be necessary.
            await Task.FromResult(0);
        }

        private int getRandomPersonality()
        {
            // IMPLENTATION NOTES:
            // STEP 2: - Adde the personality assignment with the percentage possibility to this function
            // This will make it available as scenario that can happen.

            // First we get a random number
            int randomPersonality = RandomUtils.GetRandomNumber(1, 101);
            int randomReaction = RandomUtils.GetRandomNumber(1, 101);

            // Now we return a type of personality based on the number.
            // First we will determine if the personality is lawful, evil or coward
            if (randomPersonality <= 70) // 60% chance of being lawful
            {
                if (randomReaction >= 1 && randomReaction < 70) return PERSONALITY.LAWFUL.STAY;
                else if (randomReaction > 70 && randomReaction <= 80) return PERSONALITY.LAWFUL.EXIT_VEHICLE;
                else if (randomReaction > 80 && randomReaction <= 90) return PERSONALITY.LAWFUL.WALK_TOWARDS_OFFICER;
                else if (randomReaction > 90 && randomReaction <= 100) return PERSONALITY.LAWFUL.WALK_AROUND;
            }   
            else if (randomPersonality >= 70 && randomPersonality < 90) // 15% chance of being a coward
            {
                if (randomReaction >= 1 && randomReaction <= 25) return PERSONALITY.COWARD.FLEE_ON_FOOT;
                else if (randomReaction > 25 && randomReaction <= 55) return PERSONALITY.COWARD.VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE;
                else if (randomReaction > 55 && randomReaction <= 85) return PERSONALITY.COWARD.VEHICLE_FLEE_AT_RANDOM;
                else if (randomReaction > 85 && randomReaction <= 100) return PERSONALITY.COWARD.AIM_SUICIDE;
            }
            else if (randomPersonality >= 90 && randomPersonality <= 100) // And if the result is between 86 and 100 it will be evil
            {
                if (randomReaction >= 1 && randomReaction <= 20) return PERSONALITY.EVIL.SHOOT_WHEN_CLOSE;
                else if (randomReaction > 20 && randomReaction <= 30) return PERSONALITY.EVIL.WALK_TOWARDS_SHOOTING;
                else if (randomReaction > 30 && randomReaction <= 40) return PERSONALITY.EVIL.FLEE_THEN_SHOOT;
                else if (randomReaction > 40 && randomReaction <= 60) return PERSONALITY.EVIL.SHOOT_AT_RANDOM_TIME;
                else if (randomReaction > 60 && randomReaction <= 70) return PERSONALITY.EVIL.SHOOT_FROM_START;
                else if (randomReaction > 80 && randomReaction <= 90) return PERSONALITY.EVIL.ATTACK_WITH_MELEE;
                else if (randomReaction > 90 && randomReaction <= 100) return PERSONALITY.EVIL.SHOOT_WHEN_OFFICER_IS_OUT;
            }

            // If for some reason it didn't fall under any other category, then we will let it continue as usual.
            return PERSONALITY.LAWFUL.STAY;
        }

        private async Task triggerScenario(int PERSONALITY_TYPE, Ped targetPed, Ped player, Vehicle targetVehicle)
        {
            // IMPLEMENTATION NOTE
            // STEP 3: Add your scenario details here. Match the PERSONALITY_TYPE with the type of Personality in the CLASS that identifies your scenario.

            /* -------------------------------------------------
               -------------------------------------------------
               --------------------EVIL BLOCK-------------------           
               -------------------------------------------------
               ------------------------------------------------- */


            if (PERSONALITY_TYPE == PERSONALITY.EVIL.ATTACK_WITH_MELEE)
            {
                // SCENARIO DESCRIPTION:
                // In this scenario, the PED will attack the player at a random moment with either their fist or a melee weapon.
                // Here we are setting a random timer for the action to begin.
                await( Delay(RandomUtils.GetRandomNumber(1000, 30000)));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.


                // Here we will do a randomizer to determine if the ped will attack with a melee weapon, or with their bare fist.
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                // 50/50 Chance of attacking with a melee weapon or fist
                if (randomNumber > 50)
                {
                    // Here we will check if the PED is in a vehicle. If they are, they can get a bigger melee weapon
                    if (targetPed.IsInVehicle())
                    {
                        targetPed.Weapons.Give(GetMeleeWeapon(), 1, true, true);
                    }
                    else
                    { // If they are not, they can only get a concealable blade.
                        targetPed.Task.PlayAnimation(ANIMATION_TYPE.UNHOLSTER_MELEE, "unholster", 8f, -1, AnimationFlags.None);
                        targetPed.Weapons.Give(GetConcealedBlade(), 1, true, true);
                    }
                }

                // Here we will check if the PED is still in the vehicle. If he is, we will take them out of the car.
                if (targetPed.IsInVehicle())
                {
                    // Now we ask the ped to leave the vehicle
                    targetPed.Task.LeaveVehicle();

                    // Now we allowing time for the driver to get out of the vehicle so the instruction can execute.
                    await BaseScript.Delay(1500);
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                }

                // Lastly, we fight!
                targetPed.Task.FightAgainst(player);


                // This whole block is in case the PED kills the officer, it will flee away in his car after 20 seconds
                await (BaseScript.Delay(TIME.SECONDS_20));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Here we will make the suspect run back to their car, otherwise they will just walk to it and might miss getting back before fleeing.
                targetPed.Task.ClearAll();
                targetPed.Task.RunTo(targetVehicle.Position);
                await (BaseScript.Delay(3000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                targetPed.Task.ClearAll();
                targetPed.Task.EnterVehicle(targetVehicle, VehicleSeat.Driver);
                await (BaseScript.Delay(3000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                targetPed.Task.FleeFrom(player);

                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////
            
            if (PERSONALITY_TYPE == PERSONALITY.EVIL.SHOOT_WHEN_CLOSE)
            {
                bool exit = false;
                while (!exit)
                {
                    // For optimization purposes
                    await BaseScript.Delay(1000);
                    // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                    // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                    if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                    // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                    if(World.GetDistance(player.Position, targetPed.Position) <= 3f) { exit = true; }
                    
                }

                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // First we equip a random handgun
                targetPed.Weapons.Give(getHandgun(), 200, true, true);

                // Now we will roll down the window to avoid the PED breaking the window to shoot.
                API.RollDownWindow(targetVehicle.Handle, 0);

                // Now we set the shooting rate to be fast
                targetPed.ShootRate = 1000;
                // And lastly, we trigger the action to shoot at the player
                targetPed.Task.VehicleShootAtPed(player);

                // Now we allowing time before the PED flees
                await BaseScript.Delay(6000);
                if(isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                // Lastly, the ped flees away
                targetPed.Task.FleeFrom(player);


                /////////////

                // Here we will upgrade this scenario with an extra level or randomness.
                // With a 50/50 chance, the ped will flee forever until caught, or will get out of the vehicle to fight, or flee on foot.
                int randomUpgrade = RandomUtils.GetRandomNumber(1, 101);

                if(randomUpgrade > 50)
                {
                    // Now we will check if we will make him fight or escape on foot
                    if (randomUpgrade >= 51 && randomUpgrade <= 75)
                    {
                        // First we wait some time from the ped fleeing from the officer
                        await (Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_30, TIME.MINUTES_5)));
                        // Now we make the ped get out of the vehicle
                        targetPed.Task.LeaveVehicle();
                        await (BaseScript.Delay(2500));
                        if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                        targetPed.Task.ClearAll();
                        targetPed.Task.HandsUp(30000);
                        
                        // Extra scenario with 5% chance for the ped to try to run on foot after being stopped, or shooting back at the player.
                        if(randomUpgrade >=51 && randomUpgrade <= 55)
                        {
                            await (Delay(RandomUtils.GetRandomNumber(10000, 15000)));
                            if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                            targetPed.Task.ReactAndFlee(player);
                        } 
                        else if (randomUpgrade >= 71 && randomUpgrade <= 75)
                        {
                            await (Delay(RandomUtils.GetRandomNumber(5000, 10000)));
                            if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                            targetPed.Task.ShootAt(player);
                        }
                    }
                    
                }
                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////
            
            if (PERSONALITY_TYPE == PERSONALITY.EVIL.SHOOT_FROM_START)
            {
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // First we equip a random handgun
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if (randomNumber < 80)
                { // 80% chance of it getting a handgun
                    targetPed.Weapons.Give(getHandgun(), 200, true, true);
                }
                else
                { // 20% chance of it getting an assault rifle instead
                    targetPed.Weapons.Give(getAssaultRifle(), 200, true, true);
                }

                // Now we will make the ped get out of the car
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(1500));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we set the shooting rate to be fast
                targetPed.ShootRate = 1000;
                // And lastly, we trigger the action to shoot at the player
                targetPed.Task.ShootAt(player);

                await (BaseScript.Delay(TIME.SECONDS_20));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.


                // This whole block is in case the PED kills the officer, it will flee away in his car after 20 seconds
                targetPed.Task.EnterVehicle(targetVehicle, VehicleSeat.Driver);
                await (BaseScript.Delay(3000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                targetPed.Task.FleeFrom(player);
                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////

            if (PERSONALITY_TYPE == PERSONALITY.EVIL.SHOOT_WHEN_OFFICER_IS_OUT)
            {
                // This is to stop the PED from getting distracted with world events
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // Here we are going to wait for the player to get out of the car.
                while (Game.PlayerPed.IsInVehicle())
                {
                    // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                    // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                    if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                    await BaseScript.Delay(1000);
                }
                // Checking if the ped exists after every delay
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we need to equip the ped with a gun, we will assign one based on odds
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if (randomNumber < 80)
                { // 80% chance of it getting a handgun
                    targetPed.Weapons.Give(getHandgun(), 200, true, true);
                }
                else
                { // 20% chance of it getting an assault rifle instead
                    targetPed.Weapons.Give(getAssaultRifle(), 200, true, true);
                }


                // Now we will make the ped get out of the car
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(1500));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we set the shooting rate to be fast
                targetPed.ShootRate = 1000;
                // And lastly, we trigger the action to shoot at the player
                targetPed.Task.ShootAt(player);

                await (BaseScript.Delay(TIME.SECONDS_20));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.


                // This whole block is in case the PED kills the officer, it will flee away in his car after 20 seconds
                targetPed.Task.EnterVehicle(targetVehicle, VehicleSeat.Driver);
                await (BaseScript.Delay(3000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                targetPed.Task.FleeFrom(player);
                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////

            if (PERSONALITY_TYPE == PERSONALITY.EVIL.SHOOT_AT_RANDOM_TIME)
            {
                // The goal of this scenario is for the ped to shoot deeper into the interaction with the officer if certain conditions apply.
                // This is to stop the PED from getting distracted with world events
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // Here we are going to do a random timer for the event to start. This will allow time to develop for the traffic stop.
                await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_30, TIME.MINUTES_3)));

                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we need to equip the ped with a gun, we will assign one based on odds and the status of the scenario
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                // Here we'll have an extra variable that will determine if the weapon that the suspect got was a melee weapon or a firearm
                bool weaponIsMelee = false;
                // Here we need to check if the ped is in a car or is on foot
                if (targetPed.IsInVehicle())
                {
                    // This first check is in case the ped is in a different vehicle than the one he was driving (this could mean that they have been arrested)
                    if(targetPed.CurrentVehicle.NetworkId != targetVehicle.NetworkId)
                    {
                        return;
                    }

                    // In this section we will determine if the ped will get a handgun or an assault rifle (only available when in vehicle).
                    if (randomNumber >= 1 && randomNumber <= 30)
                    { // 80% chance of it getting a handgun
                        targetPed.Weapons.Give(GetConcealedBlade(), 1, true, true);
                        weaponIsMelee = true;
                    }
                    else if (randomNumber > 30 && randomNumber <= 80)
                    { // 80% chance of it getting a handgun
                        targetPed.Weapons.Give(getHandgun(), 200, true, true);
                    }
                    else
                    { // 20% chance of it getting an assault rifle instead
                        targetPed.Weapons.Give(getAssaultRifle(), 200, true, true);
                    }
                } 
                else
                {   // Here we are checking if the ped is currently cuffed
                    if (!targetPed.IsCuffed)
                    {
                        if (randomNumber >= 1 && randomNumber <= 50)
                        { // 50% chance of it getting a handgun
                            targetPed.Task.PlayAnimation(ANIMATION_TYPE.UNHOLSTER_MELEE, "unholster", 8f, -1, AnimationFlags.None);
                            targetPed.Weapons.Give(getHandgun(), 200, true, true);                          
                        } 
                        else
                        {
                            targetPed.Task.PlayAnimation(ANIMATION_TYPE.UNHOLSTER_PISTOL, "unholster", 8f, -1, AnimationFlags.None);
                            targetPed.Weapons.Give(GetConcealedBlade(), 1, true, true);
                            weaponIsMelee = true;
                        }
                        
                    } 
                    else
                    {
                        return;
                    }
                }

                if (targetPed.IsInVehicle())
                {
                    // Now we will make the ped get out of the car
                    targetPed.Task.LeaveVehicle();
                    await (BaseScript.Delay(3000));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                }

                // Now we set the shooting rate to be fast
                targetPed.ShootRate = 1000;
                // We are getting ready to shoot, so we will cancel all other tasks.
                targetPed.Task.ClearAll();
                await (BaseScript.Delay(1000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                // And lastly, we trigger the action to shoot at the player
                // This is done this way because if the weapon is melee, it won't engage at the player with ShootAt function
                if (weaponIsMelee)
                {
                    targetPed.Task.FightAgainst(player);
                } 
                else
                {
                    targetPed.Task.ShootAt(player);
                }
                             

                await (BaseScript.Delay(TIME.SECONDS_20));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.


                // This whole block is in case the PED kills the officer, it will flee away in his car after 20 seconds
                targetPed.Task.EnterVehicle(targetVehicle, VehicleSeat.Driver);
                await (BaseScript.Delay(3000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                targetPed.Task.FleeFrom(player);
                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////

            if (PERSONALITY_TYPE == PERSONALITY.EVIL.FLEE_THEN_SHOOT)
            {
                // The goal of this scenario is for the ped to shoot deeper into the interaction with the officer if certain conditions apply.
                // This is to stop the PED from getting distracted with world events
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // Here we will determine if the PED will flee at a random time, when the officer gets closer to the window, or when the officer leaves their vehicle
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if(randomNumber <= 33)
                { // The first 33% chance will be that the ped will wait a random amount of time
                  // Here we are going to do a random timer for the event to start. This will allow time to develop for the traffic stop.
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_30)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                } 
                else if(randomNumber > 33 && randomNumber <= 66) // The second 50% will wait for the officer to get close to the window
                {
                    while (true)
                    {
                        // For optimization purposes
                        await BaseScript.Delay(1000);
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                        if (World.GetDistance(player.Position, targetPed.Position) <= 3f) { break; }

                    }
                } 
                else
                {
                    while (Game.PlayerPed.IsInVehicle())
                    {
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        await BaseScript.Delay(1000);
                    }
                }

                // Now we need to equip the ped with a gun, we will assign one based on odds and the status of the scenario
                // Here we need to check if the ped is in a car or is on foot
                if (targetPed.IsInVehicle())
                {
                    // This first check is in case the ped is in a different vehicle than the one he was driving (this could mean that they have been arrested)
                    if (targetPed.CurrentVehicle.NetworkId != targetVehicle.NetworkId)
                    {
                        return;
                    }

                    // In this section we will determine if the ped will get a handgun or an assault rifle (only available when in vehicle).
                    if (randomNumber > 1 && randomNumber <= 80)
                    { // 80% chance of it getting a handgun
                        targetPed.Weapons.Give(getHandgun(), 200, true, true);
                    }
                    else
                    { // 20% chance of it getting an assault rifle instead
                        targetPed.Weapons.Give(getAssaultRifle(), 200, true, true);
                    }
                }
                else
                {   // Here we are checking if the ped is currently cuffed
                    if (!targetPed.IsCuffed)
                    {
                        targetPed.Task.PlayAnimation(ANIMATION_TYPE.UNHOLSTER_PISTOL, "unholster", 8f, -1, AnimationFlags.None);
                        targetPed.Weapons.Give(getHandgun(), 200, true, true);
                    }
                    else
                    {
                        return;
                    }
                }

                // Now we will make the PED flee in whatever he is.
                targetPed.Task.FleeFrom(player);

                // We will keep the PED fleeing for a random amount of time
                await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_30, TIME.MINUTES_1)));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                if (targetPed.IsInVehicle())
                {
                    // Now we will make the ped get out of the car
                    targetPed.Task.LeaveVehicle();
                    await (BaseScript.Delay(3000));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                    // Now we will add an option where the ped might run on foot, or start the confrontation right away
                    if (randomNumber < 50)
                    {
                        targetPed.Task.ClearAll();
                        targetPed.Task.FleeFrom(player);
                        await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_30)));
                        if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                    }
                }

                // Now we set the shooting rate to be fast
                targetPed.ShootRate = 1000;
                // We are getting ready to shoot, so we will cancel all other tasks.
                targetPed.Task.ClearAll();
                await (BaseScript.Delay(1000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                // And lastly, we trigger the action to shoot at the player
                targetPed.Task.ShootAt(player);

                await (BaseScript.Delay(TIME.SECONDS_20));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.


                // This whole block is in case the PED kills the officer, it will flee away in his car after 20 seconds
                targetPed.Task.EnterAnyVehicle(VehicleSeat.Driver);
                await (BaseScript.Delay(3000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                targetPed.Task.FleeFrom(player);
                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////

            if (PERSONALITY_TYPE == PERSONALITY.EVIL.WALK_TOWARDS_SHOOTING)
            {
                // Scenario Description:
                // In this scenario the PED will exit the vehicle right away after the traffic stop, when the officer gets closer to the window, or at a random time.

                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;


                // Here we will determine if the PED will exit at a random time, when the officer gets closer to the window, or when the officer leaves their vehicle
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if (randomNumber <= 50)
                { // The first 33% chance will be that the ped will wait a random amount of time
                  // Here we are going to do a random timer for the event to start. This will allow time to develop for the traffic stop.
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_45)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                }
                else
                {
                    while (Game.PlayerPed.IsInVehicle())
                    {
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        await BaseScript.Delay(1000);
                    }
                }

                // We are adding this code here because all three scenarios will result in the same action.
                // Now we will make the PED leave the vehicle
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(1500));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Here we will add a little bit of variety by allowing the ped to sometimes walk to the officer, and sometimes run to the officer
                if (randomNumber % 2 == 0)
                { // Here we are making the ped go to the officer
                    targetPed.Task.GoTo(player);
                }
                else
                {
                    targetPed.Task.RunTo(player.Position);
                }

                // With this we are adding a delay where the player will keep walking up to the player until they are close enough so they cna stop
                while (true)
                {
                    // For optimization purposes
                    await BaseScript.Delay(500);
                    // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                    // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                    if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                    // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                    if (World.GetDistance(player.Position, targetPed.Position) <= 7f) { break; }

                }

                targetPed.Task.ClearAll();
                targetPed.ShootRate = 1000;
                targetPed.Task.PlayAnimation(ANIMATION_TYPE.UNHOLSTER_PISTOL, "unholster", 8f, -1, AnimationFlags.None);
                targetPed.Weapons.Give(getHandgun(), 200, true, true);
                targetPed.Task.ShootAt(player);

                // We are going to make the shooting last 20 seconds before the suspect chooses to flee
                await BaseScript.Delay(20000);
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Here we are going to make the suspect flee. It will be a 50/50 chance that they will flee on foot, or on their vehicle
                if(randomNumber % 2 == 0)
                { // In this case, it will flee on their vehicle
                    // Fist we will tell the PED to run back to their vehicle
                    targetPed.Task.RunTo(targetVehicle.Position);

                    // Now we will do a little loop checking that it is close enough to the vehicle so they can join in
                    while (true)
                    {
                        // For optimization purposes
                        await BaseScript.Delay(500);
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                        if (World.GetDistance(targetVehicle.Position, targetPed.Position) <= 3f) { break; }

                    }

                    // After the PED is close enough to the vehicle, we will task them with entering the vehicle
                    targetPed.Task.EnterVehicle(targetVehicle, VehicleSeat.Driver);
                    await BaseScript.Delay(3000);
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                }

                // And now they will flee, using the vehicle if they are inside, or on foot if they are not.
                targetPed.Task.ClearAll();
                targetPed.Task.FleeFrom(player);
                return;
            }

            /* -------------------------------------------------
            -------------------------------------------------
            -------------------COWARD BLOCK------------------           
            -------------------------------------------------
            ------------------------------------------------- */

            if (PERSONALITY_TYPE == PERSONALITY.COWARD.VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE)
            {
                // The goal of this scenario is for the ped to flee after the officer either leaves the vehicle or when they are getting closer to the window
                // This is to stop the PED from getting distracted with world events
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // Here we will determine if the PED will flee at a random time, when the officer gets closer to the window, or when the officer leaves their vehicle
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if (randomNumber <= 50)
                { // 50% Chance for the PED to flee once the officer gets close to the window
                    while (true)
                    {
                        // For optimization purposes
                        await BaseScript.Delay(1000);
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                        if (World.GetDistance(player.Position, targetPed.Position) <= 3f) { break; }

                    }
                }
                else // 50% chance for the PED to flee once the officer gets out of their vehicle
                {
                    while (Game.PlayerPed.IsInVehicle())
                    {
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        await BaseScript.Delay(1000);
                    }
                }

                // Now we will send a message alerting the player that the ped is about to flee
                Screen.ShowSubtitle(getFleeingMessage());
                // Now we will make the PED flee from player
                targetPed.Task.FleeFrom(player);
                // We will keep the PED fleeing for a random amount of time
                await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_30, TIME.MINUTES_3)));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we will do a second randomizer where we will determine if the pursuit will be until the PED is stopped, or if they will get out of the vehicle to flee on foot
                // We will use MOD for optimization
                if (randomNumber % 2 == 0)
                {
                    if (targetPed.IsInVehicle())
                    {
                        // Now we will make the ped get out of the car
                        targetPed.Task.LeaveVehicle();
                        await (BaseScript.Delay(3000));
                        if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                        // Now the ped will flee on foot

                        // The following blocks are to create a sense of desperation from the PED running away
                        targetPed.Task.ClearAll();
                        targetPed.Task.ReactAndFlee(player);
                        await (BaseScript.Delay(5000));
                        if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                        targetPed.Task.ClearAll();
                        targetPed.Task.ReactAndFlee(player);
                        await (BaseScript.Delay(5000));
                        if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                        targetPed.Task.ClearAll();
                        targetPed.Task.ReactAndFlee(player);
                        await (BaseScript.Delay(5000));
                        if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                    }
                }

                if (randomNumber >= 1 && randomNumber < 33)
                {   // In this case, ped will surrender
                    targetPed.Task.ClearAll();
                    targetPed.Task.HandsUp(60000);
                }
                else if (randomNumber >= 33 && randomNumber <= 66)
                { // In this case, ped will commit suicide
                    targetPed.Task.PlayAnimation(ANIMATION_TYPE.UNHOLSTER_PISTOL, "unholster", 8f, -1, AnimationFlags.None);
                    targetPed.Weapons.Give(WeaponHash.Pistol, 200, true, true);
                    targetPed.Task.ClearAll();                   
                    targetPed.Task.HandsUp(2000);
                    await (BaseScript.Delay(2000));
                    targetPed.Task.PlayAnimation("mp_suicide", "pistol", 8f, -1, AnimationFlags.StayInEndFrame);
                    await (BaseScript.Delay(700));
                    API.SetPedShootsAtCoord(targetPed.Handle, targetPed.Position.X, targetPed.Position.Y + 3, targetPed.Position.Z, true);
                    await (BaseScript.Delay(1000));
                    targetPed.Kill();
                }
                else
                {
                    targetPed.Task.ReactAndFlee(player);
                }
                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////
            
            if (PERSONALITY_TYPE == PERSONALITY.COWARD.FLEE_ON_FOOT)
            {
                // The goal of this scenario is for the PED to flee on foot as soon as he is getting pulled over
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // First we will make the PED leave the vehicle
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(1500));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we will make the PED flee in fear
                targetPed.Task.ReactAndFlee(player);

                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////

            if (PERSONALITY_TYPE == PERSONALITY.COWARD.VEHICLE_FLEE_AT_RANDOM)
            {
                // The goal of this scenario is for the PED to flee on foot as soon as he is getting pulled over
                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // First we will set a random timer for the ped to flee
                await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_30, TIME.MINUTES_1)));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we will send a message alerting the player that the ped is about to flee
                Screen.ShowSubtitle(getFleeingMessage());
                // Now we will make the ped flee
                targetPed.Task.FleeFrom(player);

                // Now we will add a randomizer to allow the PED to surrender after a while, or not 
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                // 50% Chance that the pursuit will last for a little bit and the suspect will get out and surrender.
                if(randomNumber >= 50)
                {
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.MINUTES_1, TIME.MINUTES_3)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                    targetPed.Task.LeaveVehicle();
                    await (BaseScript.Delay(3000));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                    targetPed.Task.HandsUp(60000); 
                }

                return;
            }


            ////////////////
            // NEXT SCENARIO
            ////////////////

            if (PERSONALITY_TYPE == PERSONALITY.COWARD.AIM_SUICIDE)
            {
                // Scenario Description:
                // In this scenario, the PED will get out of the vehicle after a brief time with a gun aimed at the player
                // Then it will decide if it will surrender or it will commit suicide.

                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;

                // First we will give the ped a pistol
                targetPed.Weapons.Give(WeaponHash.Pistol, 200, true, true);

                // Now we will set the time for the PED to decide to get out of the vehicle.
                await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_5, TIME.SECONDS_10)));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we will make the PED leave the vehicle
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(2000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we will make the PED aim at the player
                targetPed.Task.AimAt(player, 10000);

                // Now we need to decide with a randomizer what will the PED do
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if(randomNumber < 50)
                {
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_10, TIME.SECONDS_15)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                    targetPed.Task.ClearAll();
                    targetPed.Task.HandsUp(60000);
                } 
                else
                {
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_5, TIME.SECONDS_15)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                    targetPed.Task.ClearAll();
                    targetPed.Task.PlayAnimation("mp_suicide", "pistol", 8f, -1, AnimationFlags.StayInEndFrame);
                    await (BaseScript.Delay(700));
                    API.SetPedShootsAtCoord(targetPed.Handle, targetPed.Position.X, targetPed.Position.Y+3, targetPed.Position.Z, true);
                    await (BaseScript.Delay(1000));
                    targetPed.Kill();

                }

                return;
            }

            /* -------------------------------------------------
            -------------------------------------------------
            -------------------LAWFUL BLOCK------------------           
            -------------------------------------------------
            ------------------------------------------------- */

            if (PERSONALITY_TYPE == PERSONALITY.LAWFUL.STAY)
            {
                // Scenario Description:
                // This is the base scenario where the ped will stay on their vehicle during the enterity of the interaction

                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////

            if (PERSONALITY_TYPE == PERSONALITY.LAWFUL.EXIT_VEHICLE)
            {
                // Scenario Description:
                // In this scenario the PED will exit the vehicle right away after the traffic stop, when the officer gets closer to the window, or at a random time.

                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;


                // Here we will determine if the PED will exit at a random time, when the officer gets closer to the window, or when the officer leaves their vehicle
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if (randomNumber <= 33)
                { // The first 33% chance will be that the ped will wait a random amount of time
                  // Here we are going to do a random timer for the event to start. This will allow time to develop for the traffic stop.
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_5, TIME.SECONDS_30)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                    
                }
                else if (randomNumber > 33 && randomNumber <= 66) // The second 33% will wait for the officer to get close to the window
                {
                    while (true)
                    {
                        // For optimization purposes
                        await BaseScript.Delay(1000);
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                        if (World.GetDistance(player.Position, targetPed.Position) <= 3f) { break; }

                    }
                }
                else // This one will wait for the player to get out of their vehicle
                {
                    while (Game.PlayerPed.IsInVehicle())
                    {
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        await BaseScript.Delay(1000);
                    }
                }

                // We are adding this code here because all three scenarios will result in the same action.
                // Now we will make the PED leave the vehicle
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(1000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.
                // Now we will make the ped walk towards teh officer
                targetPed.Task.GoTo(player.Position);
                await (BaseScript.Delay(1500));
                // Now we will make the ped face the player.
                targetPed.Task.ClearAll();
                await (BaseScript.Delay(500));
                targetPed.Task.TurnTo(player);
                await (BaseScript.Delay(1500));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                targetPed.Task.PlayAnimation(ANIMATION_TYPE.COMPLAIN, getComplainAnimation(), 8f, -1, AnimationFlags.None);
                await (BaseScript.Delay(2000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                targetPed.Task.ClearAll();
                targetPed.Task.LookAt(player, 60000);
                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////
            
            if (PERSONALITY_TYPE == PERSONALITY.LAWFUL.WALK_TOWARDS_OFFICER)
            {
                // Scenario Description:
                // In this scenario the PED will exit the vehicle right away after the traffic stop, when the officer gets closer to the window, or at a random time.

                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;


                // Here we will determine if the PED will exit at a random time, when the officer gets closer to the window, or when the officer leaves their vehicle
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if (randomNumber <= 50)
                { // The first 33% chance will be that the ped will wait a random amount of time
                  // Here we are going to do a random timer for the event to start. This will allow time to develop for the traffic stop.
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_45)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                }            
                else
                {
                    while (Game.PlayerPed.IsInVehicle())
                    {
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        await BaseScript.Delay(1000);
                    }
                }

                // We are adding this code here because all three scenarios will result in the same action.
                // Now we will make the PED leave the vehicle
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(1500));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Here we will add a little bit of variety by allowing the ped to sometimes walk to the officer, and sometimes run to the officer
                if (randomNumber % 2 == 0)
                { // Here we are making the ped go to the officer
                    targetPed.Task.GoTo(player);
                }
                else
                {
                    targetPed.Task.RunTo(player.Position);
                }

                // With this we are adding a delay where the player will keep walking up to the player until they are close enough so they cna stop
                while (true)
                {
                    // For optimization purposes
                    await BaseScript.Delay(1000);
                    // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                    // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                    if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                    // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                    if (World.GetDistance(player.Position, targetPed.Position) <= 3f) { break; }

                }

                targetPed.Task.ClearAll();
                targetPed.Task.TurnTo(player);
                await BaseScript.Delay(1000);
                targetPed.Task.LookAt(player, 60000);

                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////
            
            if (PERSONALITY_TYPE == PERSONALITY.LAWFUL.WALK_AROUND)
            {
                // Scenario Description:
                // In this scenario the PED will exit the vehicle right away after the traffic stop, when the officer gets closer to the window, or at a random time.

                targetPed.AlwaysKeepTask = true;
                targetPed.BlockPermanentEvents = true;


                // Here we will determine if the PED will exit at a random time, when the officer gets closer to the window, or when the officer leaves their vehicle
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

                if (randomNumber <= 33)
                { // The first 33% chance will be that the ped will wait a random amount of time
                  // Here we are going to do a random timer for the event to start. This will allow time to develop for the traffic stop.
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_45)));
                    if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                }
                else if (randomNumber > 33 && randomNumber <= 66) // The second 50% will wait for the officer to get close to the window
                {
                    while (true)
                    {
                        // For optimization purposes
                        await BaseScript.Delay(1000);
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        // Here we are asking how far is the player from the ped. If it is too far we will ask again in 1 second, if not, we will continue with the function.
                        if (World.GetDistance(player.Position, targetPed.Position) <= 3f) { break; }

                    }
                }
                else
                {
                    while (Game.PlayerPed.IsInVehicle())
                    {
                        // This is done to avoid the script getting stuck on loop if the player glitches the system activating too many traffic stops too close
                        // If they do that, the mess the PED information and the script loses the reference of which PED it is using.
                        if (!Utilities.IsPlayerPerformingTrafficStop() || isPedEmpty(targetPed)) { return; }

                        await BaseScript.Delay(1000);
                    }
                }

                // We are adding this code here because all three scenarios will result in the same action.
                // Now we will make the PED leave the vehicle
                targetPed.Task.LeaveVehicle();
                await (BaseScript.Delay(1500));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we will make the ped walk around the area. We will use a little randomizer to determine if they will guard it, or if they will just wander
                if (randomNumber % 2 == 0)
                { // Here we are making the ped go to the officer
                    targetPed.Task.WanderAround();
                }
                else
                {
                    targetPed.Task.GuardCurrentPosition();
                }

                await (BaseScript.Delay(30000));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                targetPed.Task.ClearAll();
                targetPed.Task.TurnTo(player);
                await (BaseScript.Delay(1000));
                targetPed.Task.LookAt(player, 60000);

                return;
            }

            ////////////////
            // NEXT SCENARIO
            ////////////////
        }

        private bool isPedEmpty(Ped targetPed)
        {
            if (targetPed == null || targetPed.NetworkId == 0)
                return true; 
            else 
                return false;
        }

        private WeaponHash GetMeleeWeapon()
        {
            List<WeaponHash> weapons = new List<WeaponHash>()
            {
                WeaponHash.Bat,
                WeaponHash.Wrench,
                WeaponHash.Bottle,
                WeaponHash.Crowbar,
                WeaponHash.GolfClub,
                WeaponHash.PoolCue,
                WeaponHash.Machete,
                WeaponHash.Hatchet,
            };

            return weapons.SelectRandom();
        }

        private WeaponHash GetConcealedBlade()
        {
            List<WeaponHash> weapons = new List<WeaponHash>()
            {
                WeaponHash.Knife,
                WeaponHash.SwitchBlade,
            };

            return weapons.SelectRandom();
        }

        private WeaponHash getHandgun()
        {
            List<WeaponHash> weapons = new List<WeaponHash>()
            {
                WeaponHash.CombatPistol,
                WeaponHash.HeavyPistol,
                WeaponHash.MicroSMG,
                WeaponHash.Pistol,
                WeaponHash.Pistol50,
                WeaponHash.Revolver,
            };

            return weapons.SelectRandom();
        }

        private WeaponHash getAssaultRifle()
        {
            List<WeaponHash> weapons = new List<WeaponHash>()
            {
                WeaponHash.AssaultRifle,
                WeaponHash.CarbineRifle,
                WeaponHash.CompactRifle,
                WeaponHash.Gusenberg,
            };

            return weapons.SelectRandom();
        }

        private WeaponHash getHeavyWeapon()
        {
            List<WeaponHash> weapons = new List<WeaponHash>()
            {
                WeaponHash.CombatMGMk2,
            };

            return weapons.SelectRandom();
        }

        private string getComplainAnimation()
        {
            List<string> complainAnimation = new List<string>()
            {
            "bring_it_on",
            "give_me_a_break",
            "numbnuts",
            "screw_you",
            "threaten",
            };

            return complainAnimation.SelectRandom();
        }

        private string getFleeingMessage()
        {
            List<string> fleeingMessages = new List<string>()
            {
            "~r~Suspect:~s~ YOU'LL NEVER CATCH ME ALIVE!!",
            "~r~Suspect:~s~ DEFUND THE POLICE!!",
            "~r~Suspect:~s~ I DIDN'T DO NOTHING!!!",
            "~r~Suspect:~s~ WHY YOU PULLING ME OVER FOOL!??",
            "~r~Suspect:~s~ GO PULL YOUR MAMA OVER!",
            "~r~Suspect:~s~ CATCH ME IF YOU CAN!",
            "~r~Suspect:~s~ TOO SLOW GRANDMA!",
            "~r~Suspect:~s~ CHINGA A TU MADRE CABRON!!!",
            "~r~Suspect:~s~ I'M NOT DRIVING I'M TRAVELLING!",
            "~r~Suspect:~s~ YOU CAN'T STOP ME!",
            "~r~Suspect:~s~ HELP!! POLICE BRUTALITY!",
            "~r~Suspect:~s~ I CAN'T STOP I'M SHITTING MY PANTS!!",
            "~r~Suspect:~s~ VETE A LA VERGA!",
            "~r~Suspect:~s~ You really thought you'd catch me huh?",
            "~r~Suspect:~s~ I'M SORRY I'M GONNA BE LATE FOR WORK!",
            "~r~Suspect:~s~ NO NO NO NO NO!!!",
            "~r~Suspect:~s~ I'M TOO PRETTY TO GO TO JAIL!",
            "~r~Suspect:~s~ I CAN'T GO TO JAIL!!!",
            "~r~Suspect:~s~ I'M SORRY!!!",
            "~r~Suspect:~s~ SCREW YOU!",
            "~p~Suspect says to his phone:~s~ Yooo stream! Watch this!",
            "~p~Suspect shows you the middle finger and drives off",
            "~p~Suspect starts blasting loud heavy metal and drives off",
            "~p~Suspect starts blasting loud reggaeton and drives off",
            "~p~Suspect starts blasting loud music and drives off",
            "~y~AMBIENT:~s~ You hear a vehicle driving off",
            "~y~AMBIENT:~s~ You hear some tires screeching",
            };

            return fleeingMessages.SelectRandom();
        }

        private string getFleeingAfterArrestMessage()
        {
            List<string> fleeingMessages = new List<string>()
            {
                "~r~Suspect:~s~ I can't go back to jail!",
                "~r~Suspect:~s~ They’re gonna kill me in there!",
                "~r~Suspect:~s~ This isn't happening!",
                "~r~Suspect:~s~ I’m innocent! I didn’t do anything!",
                "~r~Suspect:~s~ You’ll never take me alive!",
                "~r~Suspect:~s~ I can’t let them find out!",
                "~r~Suspect:~s~ I have to see my family one last time!",
                "~r~Suspect:~s~ This is all a big mistake!",
                "~r~Suspect:~s~ You don’t understand, I have no choice!",
                "~r~Suspect:~s~ I’m not going down for this!",
                "~r~Suspect:~s~ I knew I shouldn’t have trusted you!",
                "~r~Suspect:~s~ I have to warn them before it’s too late!",
                "~r~Suspect:~s~ You can’t hold me here!",
                "~r~Suspect:~s~ They’ll never find me!",
                "~r~Suspect:~s~ This can’t be happening to me!",
                "~r~Suspect:~s~ I have to finish what I started!",
                "~r~Suspect:~s~ You won’t catch me!",
                "~r~Suspect:~s~ I can’t let them win!",
                "~r~Suspect:~s~ I have nothing left to lose!",
                "~r~Suspect:~s~ I need to disappear!",
            };

            return fleeingMessages.SelectRandom();
        }

        private string getWalkingAfterArrestMessage()
        {
            List<string> fleeingMessages = new List<string>()
            {
                "~r~Suspect:~s~ Just taking a quick stroll, no big deal!",
                "~r~Suspect:~s~ I'll be right back, just grabbing something!",
                "~r~Suspect:~s~ Don't worry, I'm not going far!",
                "~r~Suspect:~s~ I’m just going to get some air!",
                "~r~Suspect:~s~ I’ll be back in a minute, promise!",
                "~r~Suspect:~s~ I just need to stretch my legs!",
                "~r~Suspect:~s~ I think I left my phone over there!",
                "~r~Suspect:~s~ Just need to check on something real quick!",
                "~r~Suspect:~s~ I forgot something in my car!",
                "~r~Suspect:~s~ I’ll be right back, don’t worry!",
                "~r~Suspect:~s~ I’m just going to the restroom!",
                "~r~Suspect:~s~ Hold on, I just need to make a call!",
                "~r~Suspect:~s~ I need to speak to my lawyer real quick!",
                "~r~Suspect:~s~ I’m just going to grab a snack!",
                "~r~Suspect:~s~ I’m just heading to the office, be right back!",
                "~r~Suspect:~s~ I’m just stepping out for a moment!",
                "~r~Suspect:~s~ I need to check on my friend real quick!",
                "~r~Suspect:~s~ I’ll be back before you know it!",
                "~r~Suspect:~s~ I’m just going to see what’s going on over there!",
                "~r~Suspect:~s~ No worries, just taking a little walk!",
            };

            return fleeingMessages.SelectRandom();
        }

        private string getRandomCity()
        {
            List<string> allCities = new List<string>()
            {
            "San Fierro",
            "Las Venturas",
            "Liberty City",
            "Vice City",
            };

            return allCities.SelectRandom();
        }

        private string getRandomWarrant()
        {
            // First we will create a randomizer to determine if the warrant will be local or from out of state
            int localWarrantOdds = RandomUtils.GetRandomNumber(1, 101);

            string warrant = null;
            // In this case we are giving a 30% chance of it being an out of state warrant
            if(localWarrantOdds >= 70)
            {
                warrant = getRandomOutOfStateWarrant() + " from " + getRandomCity();
            } 
            else
            {
                warrant = getRandomLocalWarrant();
            }

            return warrant;
        }

        private string getRandomLocalWarrant()
        {
            List<string> allLocalWarrants = new List<string>()
            {
            "Arrest Warrant",
            "Bench Warrant",
            "Fugitive Warrant",
            "Traffic Warrant",
            "Protective Order Warrant",
            "Detention Warrant",
            "Federal Warrant",
            };

            return allLocalWarrants.SelectRandom();
        }

        private string getRandomOutOfStateWarrant()
        {
            List<string> allOutOfStateWarrants = new List<string>()
            {
            "Arrest Warrant",
            "Extradition Warrant",
            "Fugitive Warrant",
            "Foreing Arrest Warrant",
            };

            return allOutOfStateWarrants.SelectRandom();
        }

        private List<Item> getIllegalPedInventory(int scenarioType)
        {
            // First we will make a series of randomizers to determine how many illegal items will be added and what are the odds of them being added
            int triggerOdds = 0;
            int numberOfItems = 0; 
            int randomNumber = RandomUtils.GetRandomNumber(1, 101);
            int counter = 0;

            // Now we will determine the odds for everything based on the type of scenario
            if (scenarioType == SCENARIO_TYPE.LAWFUL) 
            {
                triggerOdds = ILLEGAL_INVENTORY_ODDS.LAWFUL;
                numberOfItems = RandomUtils.GetRandomNumber(0, 2); // 0 or 1 item, this is the structure
            }
            else if (scenarioType == SCENARIO_TYPE.COWARD)
            {
                triggerOdds = ILLEGAL_INVENTORY_ODDS.COWARD;
                numberOfItems = RandomUtils.GetRandomNumber(0, 3);
            }
            else if (scenarioType == SCENARIO_TYPE.EVIL)
            {
                triggerOdds = ILLEGAL_INVENTORY_ODDS.EVIL;
                numberOfItems = RandomUtils.GetRandomNumber(1, 6);
            }

            // At this point since the odds are not zero and there are items, we will start creating the item list
            Item individualItem = new Item();
            List<Item> allItems = new List<Item>();

            // Now we will start the process of adding items
            // Here we are going to check the odds of adding inventory
            if(randomNumber <= triggerOdds)
            {
                // Now we will set a counter to process the while
                counter = numberOfItems;

                // This will add the proper number of items based on previous configuration
                while(counter > 0)
                {
                    individualItem = new Item();
                    individualItem.Name = getIllegalPedItem();
                    individualItem.IsIllegal = true;
                    allItems.Add(individualItem);
                    counter--;
                }
            }

            // Here we are going to check if the trigger odds is zero or the number of items is Zero we will return so we dont mess the ped inventory
            if (allItems.Count == 0)
            {
                return null;
            }
            else
            {
                return allItems;
            }            
        }

        private List<Item> getIllegalCarInventory(int scenarioType)
        {
            // First we will make a series of randomizers to determine how many illegal items will be added and what are the odds of them being added
            int triggerOdds = 0;
            int numberOfItems = 0;
            int randomNumber = RandomUtils.GetRandomNumber(1, 101);
            int counter = 0;

            // Now we will determine the odds for everything based on the type of scenario
            if (scenarioType == SCENARIO_TYPE.LAWFUL)
            {
                triggerOdds = ILLEGAL_INVENTORY_ODDS.LAWFUL;
                numberOfItems = RandomUtils.GetRandomNumber(0, 2); // 0 or 1 item, this is the structure
            }
            else if (scenarioType == SCENARIO_TYPE.COWARD)
            {
                triggerOdds = ILLEGAL_INVENTORY_ODDS.COWARD;
                numberOfItems = RandomUtils.GetRandomNumber(0, 3);
            }
            else if (scenarioType == SCENARIO_TYPE.EVIL)
            {
                triggerOdds = ILLEGAL_INVENTORY_ODDS.EVIL;
                numberOfItems = RandomUtils.GetRandomNumber(1, 6);
            }

            // At this point since the odds are not zero and there are items, we will start creating the item list
            Item individualItem = new Item();
            List<Item> allItems = new List<Item>();

            // Now we will start the process of adding items
            // Here we are going to check the odds of adding inventory
            if (randomNumber <= triggerOdds)
            {
                // Now we will set a counter to process the while
                counter = numberOfItems;

                // This will add the proper number of items based on previous configuration
                while (counter > 0)
                {
                    individualItem = new Item();
                    individualItem.Name = getIllegalVehicleItems();
                    individualItem.IsIllegal = true;
                    allItems.Add(individualItem);
                    counter--;
                }
            }

            // Here we are going to check if the trigger odds is zero or the number of items is Zero we will return so we dont mess the ped inventory
            if (allItems.Count == 0)
            {
                return null;
            }
            else
            {
                return allItems;
            }
        }

        private string getIllegalPedItem()
        {
            int ammunitionAmount = RandomUtils.GetRandomNumber(1, 20);
            int cashAmount = RandomUtils.GetRandomNumber(2, 5);
            int randomItem = RandomUtils.GetRandomNumber(2, 5);

            List<string> allIllegalItems = new List<string>()
            {
                "Glock 19 handgun",
                "small bag of methamphetamine",
                "$" + cashAmount.ToString() + " in cash",
                "switchblade knife",
                "burner phone with recent calls to 'unknown contact'",
                "stolen credit cards under different names",
                "fake social security card",
                "small notebook with addresses and phone numbers",
                randomItem.ToString() + " prepaid SIM cards",
                "unregistered cell phone",
                "flash drive with encrypted data",
                "brass knuckles",
                "prescription pills in unlabeled bottle",
                "false-bottom water bottle",
                "key fob with hidden compartment",
                "police scanner app on phone",
                "map with marked locations",
                randomItem.ToString() + " jewelry without receipts",
                "driver's license with different names",
                "pack of cigarettes with hidden drugs",
                "handcuff keys",
                "fake business cards",
                randomItem.ToString() + " receipts from pawn shops",
                "prepaid debit cards",
                "security system jammer",
                "small bag of marijuana",
                "paper with coded messages",
                randomItem.ToString() + " gift cards under various names",
                "false documents for different identities",
                "zip ties",
                "bag of stolen mail",
                ammunitionAmount.ToString() + " loose ammunition",
                "credit card skimmer",
                "disposable cell phone",
                "black ski mask",
                "set of lock picks",
                "fake passport",
                "money clip with large bills",
                "signal jammer",
                "envelope with cash",
                randomItem.ToString() + " USB drives",
                "business card for a known criminal",
                "list of codes",
                "fake police badge",
                "counterfeit money",
                "bag of synthetic drugs",
                "set of fake IDs",
                "metal knuckles",
                "credit card reader",
                "hidden compartment wallet",
                "small vial of liquid",
                "broken bag of fentanyl",
                "bag of fentanyl",
                randomItem.ToString() + " photos of potential targets",
                "fake employee ID",
                "glass cutter",
                "hidden microphone",
                "counterfeit stamps",
                "pen with hidden camera",
                "travel-sized mouthwash with hidden compartment",
                "miniature GPS tracker",
                "counterfeit tax stamps",
                "electronic lock pick",
                "black market prescription drugs",
                "illegal steroids",
                "stolen debit cards",
                "confidential documents",
                "small bag of heroin",
                "cloning device",
                randomItem + " smuggled diamonds",
                "spy pen",
                "bag of crystal meth",
                "key fob with tracking device",
                "concealed dagger",
                randomItem + " stolen car keys",
                "counterfeit driver's licenses",
                "forged checks",
                "unregistered GPS tracker",
                "unauthorized prescription medication",
                "pocket scale",
                "money clip with counterfeit bills",
                "phone with hacked software",
                "unregistered SIM cards",
                "fake work ID",
                "forged immigration documents",
                "unregistered medical supplies",
                "set of pick tools",
                "smuggled gemstones",
                "illegal surveillance device",
                "smuggled tobacco",
                "illegal hunting tags",
                "poison vial",
                "black market seeds",
                "microfilm",
                "spy glasses",
                "mini spy camera",
                "concealed firearm magazine",
                randomItem + " stolen gift cards",
                "phone with illegal apps",
                "unregistered phone SIM",
                "hidden lock picking device",
                "forged credit cards",
                "counterfeit postage stamps",
                "surveillance photos",
                "illegal pesticide samples",
                "portable drug test kit",
                "concealed needle",
                "hidden drug paraphernalia",
                "forged legal documents",
                "smuggled cash",
                "unauthorized medical equipment",
                "concealed shiv",
                "concealed razor blade",
                "unmarked vials",
                "counterfeit licenses",
                randomItem + " smuggled prescription drugs",
                "hidden poison",
                "concealed lock picking tool",
                "illegal drug samples",
                "hidden hacking device",
                "concealed taser ring",
                "unauthorized communication device",
                "hidden firearm parts",
                randomItem + " concealed narcotics",
                "smuggled technology",
                "hidden explosive material",
                "concealed drug balloon",
                "illegal gambling slips",
                "hidden scalpel",
                "illegal hunting permits",
                "smuggled precious metals",
                "counterfeit travel documents",
                "hidden drug injection kit",
                randomItem + " concealed syringes",
                "hidden modified phone",
                "counterfeit medical supplies",
                "unregistered passport",
                "smuggled biological samples",
                "hidden handcuff key",
                "unregistered legal papers",
                "concealed RFID scanner",
                "smuggling notes",
                "counterfeit education certificates",
                "hidden GPS device",
                "FlipperZero",
                "concealed currency scanner",
                "hidden credit card duplicator",
                "unauthorized forensic equipment",
                "unregistered surveillance tools",
                "concealed drug analyzer",
                "concealed tracking chip",
                "unauthorized access cards",
                "hidden miniature explosives"
            };

            return allIllegalItems.SelectRandom();
        }

        private string getIllegalVehicleItems()
        {

            int drugKiloAmount = RandomUtils.GetRandomNumber(1, 5);
            int ammunitionAmount = RandomUtils.GetRandomNumber(1, 3000);
            int cashAmount = RandomUtils.GetRandomNumber(2, 300001);
            int randomItem = RandomUtils.GetRandomNumber(2, 5);

            List<string> allIllegalItems = new List<string>()
            {
                "AR-15 rifle",
                "Box of 9mm ammunition",
                "Box with " + ammunitionAmount.ToString() + " 5.56 rounds",
                "Box with " +ammunitionAmount.ToString() + " 7.62 rounds",
                "Box with " + ammunitionAmount.ToString() + " 9mm rounds",
                "Digital scale with residue",
                "Briefcase with $" + cashAmount.ToString(),
                drugKiloAmount.ToString() + " kilos of methamphetamine",
                drugKiloAmount.ToString() + " kilos of heroin",
                "Broken bag of Fentanyl",
                "Bag of Fentanyl",
                "Stolen laptop",
                "Stolen smartphone",
                randomItem.ToString() + " Burner phones",
                "Blueprints of a local bank",
                "Set of fake license plates",
                "Pipe bomb materials",
                "Notebook with detailed personal information on local business owners",
                "Silencer for a handgun",
                "Large bag of marijuana",
                "Fake IDs",
                "Counterfeit money printing equipment",
                "Bag of heroin",
                drugKiloAmount.ToString() + " kilos of heroin",
                "Briefcase with cash",
                randomItem.ToString() + " Stolen credit cards",
                "Hidden compartment with drugs",
                "Police scanner",
                "Forged vehicle registration",
                "Handwritten list of drug clients",
                "Box of counterfeit CDs",
                "Unregistered firearm",
                "Money laundering records",
                randomItem.ToString() + " Jewelry without receipts",
                "Stolen power tools",
                "Unmarked bottle of pills",
                "Night vision goggles",
                "Explosive device",
                "Fake passports",
                "Illegal fireworks",
                "Black duffel bag with cash",
                "Bag of cocaine",
                "Weapon cleaning kit",
                randomItem.ToString() + " Fake business cards",
                "Receipts for large cash transactions",
                "Small bag of ecstasy pills",
                "Bulletproof vest",
                "Credit card skimming device",
                "Wire cutters",
                "Cash counting machine",
                "Bag of stolen mail",
                "Key making kit",
                "Switchblade knives",
                "Large quantities of tobacco products",
                "Box of syringes",
                "Prescription medications not prescribed to the suspect",
                "Stolen car parts",
                "Empty gun holster",
                randomItem.ToString() + " Prepaid debit cards",
                "USB drive with illegal content",
                "High-powered binoculars",
                "Gasoline canister",
                "Briefcase with fake documents",
                "Bag of synthetic drugs",
                "List of contact names with illegal activities",
                "Mobile Wi-Fi jammer",
                "Stolen art pieces",
                "Large knife",
                "Bag of counterfeit goods",
                "Hidden safe with drugs",
                "Modified exhaust system for smuggling",
                "Fake police badge",
                "Illegal pesticide containers",
                randomItem.ToString() + " Lock picking sets",
                "Drug paraphernalia (pipes, bongs, etc.)",
                "Unregistered SIM cards",
                "False-bottom fuel tank",
                "Hidden compartment with cash",
                "Remote detonator",
                "Forgery equipment",
                "Multiple driver's licenses",
                "Unmarked pills",
                "Set of master keys",
                "Explosive material residue",
                "Counterfeit tax stamps",
                "Box of illegal animal products",
                "Empty plastic bags with drug residue",
                "Forgery templates",
                "Marked map of high-value targets",
                "Encrypted radio",
                "Foreign currency",
                "Smuggled cigarettes",
                "Money transfer receipts",
                "Spray paint cans with tags",
                "Rolling papers with drugs",
                "Unlabeled chemical containers",
                "Vehicle GPS tracker",
                "Box of counterfeit goods",
                "Box of prescription pads",
                "Large bag of uncut drugs",
                "Illegal wildlife parts",
                "Bag of smuggled jewels",
                "Chemicals for drug manufacturing",
                "Black market medical supplies",
                randomItem.ToString() + " Laptops with hacking software",
            };

            return allIllegalItems.SelectRandom();
        }
        private int getRandomDrivingFlag()
        {
            List<int> allFlags = new List<int>()
            {
            524732, // With this the ped will avoid traffic, vehicles, peds, objects, but will still respect traffic ligts and use blinkers
            524604, // Will avoid and won't stop at traffic lights
            524556, // Will avoid vehicles and empty vehicles but will stop at peds and objects
            786603, // Will drive normal
            1074528293, // will drive rushed
            };

            return allFlags.SelectRandom();

        }
        private async Task clearEts()
        {
            // This is to avoid ticks getting triggered 1000 times a second.
            await (BaseScript.Delay(3000));

            // First we verify if the player is currently in a ETS
            if (enhancedTrafficStop)
            {
                // Now we verify if the player is no longer on a traffic stop.
                if (!Utilities.IsPlayerPerformingTrafficStop()) 
                {
                    // If these conditions are true, it means that the player just got off an enhanced traffic stop, and we should reset his status so he can take another one.
                    enhancedTrafficStop = false;
                   
                    // Additionally, we are going to add a new task to the ped so it can go back to normal instead of just staying in the middle of the road
                    if(!isPedEmpty(tsDriver))
                    {
                        // TEST: First we are going to check if the ped is in cuffs or not. If it is in cuffs we'll let them be as we don't want to wander around nor cancel the cuffing animation
                        if (tsDriver.IsCuffed) // ALTERNATIVELY this can happen in the beginning so it doesn't clear all tasks cancelling as well the effects of putting the cuffs
                        {
                            // First we will remove all the weapons from the ped to avoid issues with the ped firing when cuffed.
                            tsDriver.Weapons.RemoveAll();
                            // Now we will generate a random number
                            int randomNumber = RandomUtils.GetRandomNumber(1, 101);
                            // If the ped is cuffed it will try to make one last attempt to run away
                            if (randomNumber <= 15)
                            {
                                // First we will clear the primary from their current task in case it is performing the arrest animation they are not left performing it while suspect runs away
                                // This IF is here just to verify that the ped is being arrested BY the person initiating the stop, otherwise it won't trigger the animation change if another officer is performing arrest
                                if (World.GetDistance(player.Position, tsDriver.Position) <= 3f)
                                { 
                                    //player.Task.PlayAnimation("move_fall", "land_fall", 8f, -1, AnimationFlags.None);
                                    player.Task.PlayAnimation("weapons@holster_1h", "holster", 8f, -1, AnimationFlags.None);
                                }
                                
                                // It will have a 5 % chance to run and a 10% chance to walk away.
                                if (randomNumber <= 5)
                                {
                                    Screen.ShowSubtitle(getWalkingAfterArrestMessage());
                                    tsDriver.Task.WanderAround();                                   
                                }
                                else
                                {
                                    Screen.ShowSubtitle(getFleeingAfterArrestMessage());
                                    tsDriver.Task.FleeFrom(player, -1);
                                }    
                            }
                            return;
                        }
                        // First we clear all it's previous tasks
                        tsDriver.Task.ClearAll();

                        // Now we determine if they are still in a car or not
                        if(tsVehicle != null && tsVehicle.NetworkId != 0)
                        {
                            // If they are in a car, we'll set them to roam arond in the car
                            tsDriver.Task.CruiseWithVehicle(tsVehicle, 30f, (int)DrivingStyle.Rushed);
                        }
                        else
                        {   // If they are on foot, we'll se them to walk away.

                            tsDriver.Task.WanderAround();
                           
                        }
                            
                    }                 

                }
            }
        }

        ///

        public async Task createSpeeder()
        {
            // We do this so the script will only execute once every 5 seconds
            await (BaseScript.Delay(30000));

            // Now we will trigger some odds of the events happening.
            int eventHappeningOdds = RandomUtils.GetRandomNumber(1, 101);
            
            // Doing it this way so it's easier to read, the number is the percentage of chance 1 to 100
            if(!(eventHappeningOdds <= 10))
            {
                return;
            }

            // Now we will select a random vehicle within 100 feet of the player
            float radius = 200.0f * 0.3048f; // The 200 is the number of feets I want to use as a reference, the other part is just so we can give the script the equivalency
            //float radius = 30f; // The 100 is the number of feets I want to use as a reference, the other part is just so we can give the script the equivalency

            // ALTERNATIVELY DO GAME POOL https://forum.cfx.re/t/how-to-get-all-vehicles-in-a-radius/4914412/4
            // Now we setup the player, the vehicle and othe variables
            player = Game.PlayerPed;
            Vector3 playerPosition = player.Position;
            int selectedRandomVehicleIdentifier;
            Vehicle randomVehicle;
            Ped randomVehicleDriver;
            bool isWanted;
            bool isPlayer;

            // Here we are selecting a random vehicle within the RADIUS
            selectedRandomVehicleIdentifier = API.GetRandomVehicleInSphere(player.Position.X, player.Position.Y, player.Position.Z, radius, 0, 70);
            randomVehicle = new Vehicle(selectedRandomVehicleIdentifier); 
            randomVehicleDriver = randomVehicle.Driver; // And here we are selecting the driver of the vehicle
            isWanted = API.IsVehicleWanted(randomVehicle.Handle);
            isPlayer = API.IsPedAPlayer(randomVehicleDriver.Handle);
            await (BaseScript.Delay(1000));

            // Now we are going to check if the vehicle selected actually has a driver, and that the driver is not the player.
            if (randomVehicleDriver == null || !randomVehicleDriver.Exists() || randomVehicleDriver == player || !isPlayer || isWanted)
            {
                // If it is we will skip this iteration
                //Screen.ShowNotification("Driver doesnt exist or is the player");
                return;
            }

            // Here we are going to mark the vehicle so it doesn't receive any further assignments from this script
            API.SetVehicleIsWanted(randomVehicle.Handle, true);

            // Now we will determine which scenario is going to execute
            int randomScenarioOdds = RandomUtils.GetRandomNumber(1, 101);
            int randomNumber = RandomUtils.GetRandomNumber(1, 101);
            int randomScenario = 0;

            if(randomScenarioOdds >= 1 && randomScenarioOdds < 40) 
            {
                randomScenario = TRAFFIC_SCENARIO.SPEEDER;
            } 
            else if (randomScenarioOdds >= 40 && randomScenarioOdds < 65)
            {
                randomScenario = TRAFFIC_SCENARIO.BROKEN_CAR;
            }
            else if (randomScenarioOdds >= 65 && randomScenarioOdds < 90)
            {
                randomScenario = TRAFFIC_SCENARIO.FLAT_TIRE;
            }
            else if (randomScenarioOdds >= 90 && randomScenarioOdds <= 100)
            {
                randomScenario = TRAFFIC_SCENARIO.STOLEN_CAR;
            }
            
            /////////////////
            /////////////////
            /////////////////

            // Now we will execute the code for each scenario
            if(randomScenario == TRAFFIC_SCENARIO.SPEEDER)
            {
                // Now that we have the vehicle and the driver we'll change the vehicle mods so it can go faster
                API.SetVehicleModKit(randomVehicle.Handle, 0); // Necessary before doing mods to the vehicle
                API.SetVehicleMod(randomVehicle.Handle, 11, 3, false); // Engine 
                API.SetVehicleMod(randomVehicle.Handle, 18, 0, false); // Turbo
                API.SetVehicleMod(randomVehicle.Handle, 13, 2, false); // Transmission 

                // And now we will clear the vehicle tasks and give it the new tasks to drive away at max speed
                API.ClearVehicleTasks(randomVehicle.Handle); // This is necessary so the vehicle will do what we tell it to do
                API.TaskVehicleDriveWander(randomVehicleDriver.Handle, randomVehicle.Handle, 100f, getRandomDrivingFlag());
                // NOTES:
                // The game doesn't allow you to modify and retain the max speed of the vehicle apart from what is set on the handling file
                // By setting the spsed to 100f we are telling the vehicle to drive at their maximum speed. So if the vehicle maximum speed is 60mph, it will do that
                // The flag of the vehicle is obtained through https://vespura.com/fivem/drivingstyle/
                return;
            } 
            else if(randomScenario == TRAFFIC_SCENARIO.BROKEN_CAR)
            {
                // This will have 2 potential scenarios, one where the car continues to drive like that and in the other one where the driver stops.
                API.SetVehicleEngineHealth(randomVehicle.Handle, 300); // With this line we're setting the car to start smoking

                if(randomNumber <= 50) // In this case the car will stop
                {
                    API.SetVehicleEngineOn(randomVehicle.Handle, false, false, true);
                    API.SetVehicleDoorOpen(randomVehicle.Handle, 4, false, false);
                    API.SetVehicleUndriveable(randomVehicle.Handle, true);
                    API.ClearVehicleTasks(randomVehicle.Handle);
                    API.ClearPedTasks(randomVehicleDriver.Handle);
                    API.TaskStandStill(randomVehicleDriver.Handle, 999999);                               
                }

                return;
            }
            else if(randomScenario == TRAFFIC_SCENARIO.STOLEN_CAR)
            {
                // And now we will clear the vehicle tasks and give it the new tasks to drive away at max speed
                API.ClearVehicleTasks(randomVehicle.Handle); // This is necessary so the vehicle will do what we tell it to do
                API.TaskVehicleDriveWander(randomVehicleDriver.Handle, randomVehicle.Handle, 100f, DRIVING_STYLES.RECKLESS_RUN_LIGHTS);
                API.SetVehicleAlarm(randomVehicle.Handle, true);
                API.StartVehicleAlarm(randomVehicle.Handle);
                API.SetVehicleIsStolen(randomVehicle.Handle, true);
            }
            else if(randomScenario == TRAFFIC_SCENARIO.FLAT_TIRE)
            {
                
                if (randomNumber <= 25) // In this case the car will stop
                {
                    API.SetVehicleTyreBurst(randomVehicle.Handle, RandomUtils.GetRandomNumber(0, 6), true, 1000f);
                    API.SetVehicleEngineOn(randomVehicle.Handle, false, false, true);
                    API.SetVehicleUndriveable(randomVehicle.Handle, true);
                    API.ClearVehicleTasks(randomVehicle.Handle);
                    API.ClearPedTasks(randomVehicleDriver.Handle);
                    API.TaskStandStill(randomVehicleDriver.Handle, 999999);
                }
                else
                {
                    API.SetVehicleTyreBurst(randomVehicle.Handle, RandomUtils.GetRandomNumber(0, 6), false, 500f);
                }
            }
        }
    }
}
