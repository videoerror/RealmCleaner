#region LICENCE
/*
Copyright 2016-2017 video_error

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */
#endregion

using System;
using System.IO;
using fCraft;

namespace RealmCleaner {

	public class Init : Plugin {

		public Init() {
		}

		private static CommandDescriptor RealmCleanCommandDescriptor;

		public void Initialize() {
			Logger.Log(LogType.ConsoleOutput, NameAndVersionAsBracket + " Loading . . .");

			RealmCleanCommandDescriptor = new CommandDescriptor {
				Name = "RealmClean",

				Aliases = new string[] {
					"Rc"
				},

				Category = CommandCategory.World | CommandCategory.Moderation,

				Permissions = new Permission[] {
					Permission.ManageWorlds
				},

				IsConsoleSafe = true,

				Usage = "/RealmClean RealmName",
				Help = "Cleans a realm by removing the realm if no BlockDB entries are found and" + 
					   "if the realm's last write date is older than a month.",

				NotRepeatable = true,

				Handler = RealmCleanHandler,
			};

			CommandManager.RegisterCustomCommand(RealmCleanCommandDescriptor);

			Logger.Log(LogType.ConsoleOutput, NameAndVersionAsBracket + " Loaded!");
		}

		public static string _Name = "RealmCleaner";
		public static string _Author = "video_error";
		public static string _Version = "0.2";
		public static string NameAndVersionAsBracket = "[" + _Name + " " + _Version + "]";

		public string Name {
			get {
				return _Name;
			}

			set {
				_Name = value;
			}
		}

		public static string Author {
			get {
				return _Author;
			}

			set {
				_Author = value;
			}
		}

		public string Version {
			get {
				return _Version;
			}

			set {
				_Version = value;
			}
		}

		private static void RealmCleanHandler(Player player, Command command) {
			string worldName = command.Next();

			if(string.IsNullOrEmpty(worldName)) {
				RealmCleanCommandDescriptor.PrintUsage(player);

				return;
			}

			World world = WorldManager.FindWorldExact(worldName);

			if(world == null) {
				RealmCleanCommandDescriptor.PrintUsage(player);

				return;
			}

			DateTime lastWorldWriteTime = File.GetLastWriteTimeUtc(Path.Combine(Paths.MapPath, world.MapFileName));

			if(world.IsRealm) {
				if(lastWorldWriteTime < DateTime.Now.AddDays(-1)) {
					BlockDBEntry[] realmBlockDBEntries = world.BlockDB.Lookup(1);

					if(realmBlockDBEntries.Length == 0) {
						try {
							WorldManager.RemoveWorld(world);
						} catch(WorldOpException worldOpException) {
							switch(worldOpException.ErrorCode) {
								case WorldOpExceptionCode.CannotDoThatToMainWorld:
									player.Message("&WWorld {0}&W is set as the main world. " +
										"Assign a new main world before deleting this one.",
									               world.ClassyName);

									return;
								
								case WorldOpExceptionCode.WorldNotFound:
									player.Message("&WWorld {0}&W is already unloaded.",
									               world.ClassyName);

									return;
								
								default:
									player.Message("&WUnexpected error occured while unloading world {0}&W: {1}",
									               world.ClassyName, worldOpException.GetType().Name);

									Logger.Log(LogType.Error,
									           "WorldCommands.WorldUnload: Unexpected error while unloading world {0}: {1}",
									           world.Name, worldOpException);

									return;
							}
						}

						WorldManager.SaveWorldList();

						Server.Message(player,
						               "{0}&S removed {1}&S from the world list.",
						               player.ClassyName, world.ClassyName);
						player.Message("Removed {0}&S from the world list. You can now delete the map file ({1}.fcm) manually.",
						               world.ClassyName, world.Name);

						Logger.Log(LogType.UserActivity,
						           "{0} removed \"{1}\" from the world list.",
						           player.Name, worldName);

						Server.RequestGC();
					}
				} else {
					player.Message("&WRealm's last edit time is not greater than a month.");
				}
			} else {
				player.Message("&WWorld is not a realm.");
			}
		}
	}
}
