using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using FivePD.API;
using FivePD.API.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public class ANIMATION_TYPE
        {
            // This is just a recollection of the animation type dictionaries that we might end up using.
            public static string COMPLAIN = "misscommon@response";
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

                    // Now we will trigger the enhanced traffic stop
                    await triggerScenario(getRandomPersonality(), tsDriver, player, tsVehicle);

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
            if (randomPersonality <= 50) // 50% chance of being lawful
            {
                if (randomReaction >= 1 && randomReaction <= 25) return PERSONALITY.LAWFUL.STAY;
                else if (randomReaction > 25 && randomReaction <= 50) return PERSONALITY.LAWFUL.EXIT_VEHICLE;
                else if (randomReaction > 50 && randomReaction <= 75) return PERSONALITY.LAWFUL.WALK_TOWARDS_OFFICER;
                else if (randomReaction > 50 && randomReaction <= 100) return PERSONALITY.LAWFUL.WALK_AROUND;
            }   
            else if (randomPersonality > 50 && randomPersonality <= 80) // 30% chance of being a coward
            {
                if (randomReaction >= 1 && randomReaction <= 25) return PERSONALITY.COWARD.FLEE_ON_FOOT;
                else if (randomReaction > 25 && randomReaction <= 50) return PERSONALITY.COWARD.VEHICLE_FLEE_AFTER_OFFICER_EXITS_VEHICLE;
                else if (randomReaction > 50 && randomReaction <= 75) return PERSONALITY.COWARD.VEHICLE_FLEE_AT_RANDOM;
                else if (randomReaction > 75 && randomReaction <= 100) return PERSONALITY.COWARD.AIM_SUICIDE;
            }
            else if (randomPersonality > 80 && randomPersonality <= 100) // And if the result is between 90 and 100 it will be evil
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

            if(PERSONALITY_TYPE == PERSONALITY.EVIL.ATTACK_WITH_MELEE)
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

                            if (!targetPed.IsCuffed)
                            {
                                targetPed.Task.ReactAndFlee(player);
                            }
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
                await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.MINUTES_1, TIME.MINUTES_5)));
                if (isPedEmpty(targetPed)) { return; } // Necessary line after every delay before further action in case the ped was emptied so it doesn't crash the script by tasking Null.

                // Now we need to equip the ped with a gun, we will assign one based on odds and the status of the scenario
                int randomNumber = RandomUtils.GetRandomNumber(1, 101);

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
                        if (randomNumber >= 1 && randomNumber <= 70)
                        { // 80% chance of it getting a handgun
                            targetPed.Weapons.Give(getHandgun(), 200, true, true);                          
                        } 
                        else
                        {
                            targetPed.Weapons.Give(GetConcealedBlade(), 1, true, true);
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
                    await (BaseScript.Delay(RandomUtils.GetRandomNumber(TIME.SECONDS_15, TIME.SECONDS_30)));
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
                // Now we will make the ped face the player.
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


    }
}
