using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json.Linq;
using TTI.TTF.Taxonomy.Model;
using TTI.TTF.Taxonomy.Model.Artifact;
using TTI.TTF.Taxonomy.Model.Core;
using Enum = System.Enum;

namespace TTI.TTF.Taxonomy.Controllers
{
	public static class TaxonomyController
	{
		private const string BaseFolder = "base";
		private const string BehaviorFolder = "behaviors";
		private const string BehaviorGroupFolder = "behavior-groups";
		private const string PropertySetFolder = "property-sets";
		private const string TokenTemplatesFolder = "token-templates";
		private const string TokenDefinitionsFolder = "token-definitions";
		
		private static readonly string _artifactPath;
		private static readonly string _folderSeparator = TxService.FolderSeparator;
		private static readonly string _latest = TxService.Latest;
		private static readonly ILog _log;

		static TaxonomyController()
		{
			Utils.InitLog();
			_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			_artifactPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + _folderSeparator +
			               TxService.ArtifactPath;
		}	
		
		#region load
		internal static Model.Taxonomy Load()
		{
			if (!Directory.Exists(_artifactPath))
			{
				var err = "Artifact Path not found: " + _artifactPath;
				_log.Error(err);
				throw new Exception(err);
			}

			_log.Info("Artifact Folder Found, loading to Taxonomy.");
			var root = new DirectoryInfo(_artifactPath);
			Model.Taxonomy taxonomy;
			var rJson = root.GetFiles("Taxonomy.json");
			var fJson = root.GetFiles("FormulaGrammar.json");
			try
			{
				taxonomy = GetArtifact<Model.Taxonomy>(rJson[0]);
				taxonomy.FormulaGrammar = GetArtifact<FormulaGrammar>(fJson[0]);
				_log.Info("Loaded Taxonomy Version: " + taxonomy.Version);
				_log.Info("Taxonomy Formula Grammar loaded");
			}
			catch (Exception e)
			{
				_log.Error("Failed to load Taxonomy: " + e);
				throw;
			}

			var aPath = _artifactPath + _folderSeparator;
			if (Directory.Exists(aPath + BaseFolder))
			{

				_log.Info("Base Artifact Folder Found, loading to Base Token Types");
				var bases = new DirectoryInfo(aPath + BaseFolder);
				foreach (var ad in bases.EnumerateDirectories())
				{
					Base baseType;
					_log.Info("Loading " + ad.Name);
					var versions = ad.GetDirectories("latest");
					var bJson = versions.FirstOrDefault()?.GetFiles("*.json");
					if (bJson != null)
					{
						try
						{
							baseType = GetArtifact<Base>(bJson[0]);
						}
						catch (Exception e)
						{
							_log.Error("Failed to load base token type: " + ad.Name);
							_log.Error(e);
							continue;
						}
					}
					else
					{
						continue;
					}

					baseType.Artifact = GetArtifactFiles(ad, baseType.Artifact);
					taxonomy.BaseTokenTypes.Add(baseType.Artifact.ArtifactSymbol.Tooling, baseType);
				}
			}
			else
			{
				_log.Error("Base artifact folder NOT found, moving on to behaviors.");
			}

			if (Directory.Exists(aPath + BehaviorFolder))
			{

				_log.Info("Behavior Artifact Folder Found, loading to Behaviors");
				var behaviors = new DirectoryInfo(aPath + BehaviorFolder);

				foreach (var ad in behaviors.EnumerateDirectories())
				{
					Behavior behavior;
					_log.Info("Loading " + ad.Name);
					var versions = ad.GetDirectories("latest");
					var bJson = versions.FirstOrDefault()?.GetFiles("*.json");
					if (bJson != null)
					{
						try
						{
							behavior = GetArtifact<Behavior>(bJson[0]);
						}
						catch (Exception e)
						{
							_log.Error("Failed to load Behavior: " + ad.Name);
							_log.Error(e);
							continue;
						}
					}
					else
					{
						continue;
					}

					behavior.Artifact = GetArtifactFiles(ad, behavior.Artifact);
					taxonomy.Behaviors.Add(behavior.Artifact.ArtifactSymbol.Tooling, behavior);
				}
			}

			if (Directory.Exists(aPath + BehaviorGroupFolder))
			{

				_log.Info("BehaviorGroup Artifact Folder Found, loading to BehaviorGroups");
				var behaviorGroups = new DirectoryInfo(aPath + BehaviorGroupFolder);
				
				foreach (var ad in behaviorGroups.EnumerateDirectories())
				{
					BehaviorGroup behaviorGroup;
					_log.Info("Loading " + ad.Name);
					var versions = ad.GetDirectories("latest");
					var bJson = versions.FirstOrDefault()?.GetFiles("*.json");
					if (bJson != null)
					{
						try
						{
							behaviorGroup = GetArtifact<BehaviorGroup>(bJson[0]);

						}
						catch (Exception e)
						{
							_log.Error("Failed to load BehaviorGroup: " + ad.Name);
							_log.Error(e);
							continue;
						}
					}
					else
					{
						continue;
					}

					behaviorGroup.Artifact = GetArtifactFiles(ad, behaviorGroup.Artifact);
					taxonomy.BehaviorGroups.Add(behaviorGroup.Artifact.ArtifactSymbol.Tooling, behaviorGroup);
				}
			}
			
			if(Directory.Exists(aPath + PropertySetFolder))
			{

				_log.Info("PropertySet Artifact Folder Found, loading to PropertySets");
				var propertySets = new DirectoryInfo(aPath + PropertySetFolder);
				foreach (var ad in propertySets.EnumerateDirectories())
				{
					PropertySet propertySet;
					_log.Info("Loading " + ad.Name);
					var versions = ad.GetDirectories("latest");
					var bJson = versions.FirstOrDefault()?.GetFiles("*.json");
					if (bJson != null)
					{
						try
						{
							propertySet = GetArtifact<PropertySet>(bJson[0]);
						}
						catch (Exception e)
						{
							_log.Error("Failed to load PropertySet: " + ad.Name);
							_log.Error(e);
							continue;
						}
					}
					else
					{
						continue;
					}
					propertySet.Artifact = GetArtifactFiles(ad, propertySet.Artifact);
					taxonomy.PropertySets.Add(propertySet.Artifact.ArtifactSymbol.Tooling, propertySet);
				}
			}

			if (!Directory.Exists(aPath + TokenTemplatesFolder)) return taxonomy;
			{
				_log.Info("TokenTemplates Folder Found, loading to TokenTemplates");
				var templateFolders = new DirectoryInfo(aPath + TokenTemplatesFolder);
				var templates = templateFolders.EnumerateDirectories();
				foreach (var t in templates)
				{
					TokenTemplate tokenTemplate;
					_log.Info("Loading " + t.Name);
					var versions = t.GetDirectories("latest");
					var bJson = versions.FirstOrDefault()?.GetFiles("*.json");
					if (bJson != null)
					{
						try
						{
							tokenTemplate = GetArtifact<TokenTemplate>(bJson[0]);

						}
						catch (Exception e)
						{
							_log.Error("Failed to load TokenTemplate: " + t.Name);
							_log.Error(e);
							continue;
						}
					}
					else
					{
						continue;
					}
					tokenTemplate.Artifact = GetArtifactFiles(t, tokenTemplate.Artifact);
					taxonomy.TokenTemplates.Add(tokenTemplate.Artifact.ArtifactSymbol.Tooling,
						tokenTemplate);
				}
			}

			if (!Directory.Exists(aPath + TokenDefinitionsFolder)) return taxonomy;
			{
				_log.Info("TokenDefinitions Folder Found, loading to TokenDefinitions");
				var definitionsFolder = new DirectoryInfo(aPath + TokenDefinitionsFolder);
				var definitions = definitionsFolder.EnumerateDirectories();
				foreach (var t in definitions)
				{
					TokenDefinition definition;
					_log.Info("Loading " + t.Name);
					var versions = t.GetDirectories("latest");
					var bJson = versions.FirstOrDefault()?.GetFiles("*.json");
					if (bJson != null)
					{
						try
						{
							definition = GetArtifact<TokenDefinition>(bJson[0]);

						}
						catch (Exception e)
						{
							_log.Error("Failed to load TokenDefinition: " + t.Name);
							_log.Error(e);
							continue;
						}
					}
					else
					{
						continue;
					}
					definition.Artifact = GetArtifactFiles(t, definition.Artifact);
					taxonomy.TokenDefinitions.Add(definition.Artifact.ArtifactSymbol.Tooling,
						definition);
				}
			}
			
			return taxonomy;
		}

		#endregion
		
		#region Create, Update, Delete
		
		public static NewArtifactResponse CreateArtifact(NewArtifactRequest artifactRequest)
		{
			var artifactJson = "";
			DirectoryInfo outputFolder = null;
		
			var jsf = new JsonFormatter(new JsonFormatter.Settings(true));
			var artifactType = artifactRequest.Type;
			var artifactName = "";
			var retVal = new NewArtifactResponse
			{
				Type = artifactType
			};
			
			switch (artifactType)
			{
				case ArtifactType.Base:
					_log.Info("CreateArtifact was requested to create a new base token type, which is not supported.");
					break;
				case ArtifactType.Behavior:
					var newBehavior = artifactRequest.Artifact.Unpack<Behavior>();
					if (!ModelManager.CheckForUniqueArtifact(ArtifactType.Behavior, newBehavior.Artifact))
					{
						newBehavior.Artifact = ModelManager.MakeUniqueArtifact(newBehavior.Artifact);
					}

					artifactName = newBehavior.Artifact.Name.ToLower();
					outputFolder = GetArtifactFolder(artifactType, artifactName);
					if(newBehavior.Artifact.ArtifactFiles.Count > 0)
						CreateArtifactFiles(newBehavior.Artifact.ArtifactFiles, outputFolder, artifactName);
					else
					{
						AddArtifactFiles(outputFolder, artifactName, "Behaviors", artifactType);
					}
					artifactJson = jsf.Format(newBehavior);
					retVal.ArtifactTypeObject= Any.Pack(newBehavior);
					break;
				case ArtifactType.BehaviorGroup:
					var newBehaviorGroup = artifactRequest.Artifact.Unpack<BehaviorGroup>();
					if (!ModelManager.CheckForUniqueArtifact(ArtifactType.BehaviorGroup, newBehaviorGroup.Artifact))
					{
						newBehaviorGroup.Artifact = ModelManager.MakeUniqueArtifact(newBehaviorGroup.Artifact);
					}
					artifactName = newBehaviorGroup.Artifact.Name.ToLower();
					outputFolder = GetArtifactFolder(artifactType, artifactName);
					if(newBehaviorGroup.Artifact.ArtifactFiles.Count > 0)
						CreateArtifactFiles(newBehaviorGroup.Artifact.ArtifactFiles, outputFolder, artifactName);
					else
					{
						AddArtifactFiles(outputFolder, artifactName, "BehaviorGroups", artifactType);
					}
					artifactJson = jsf.Format(newBehaviorGroup);
					retVal.ArtifactTypeObject= Any.Pack(newBehaviorGroup);
					break;
				case ArtifactType.PropertySet:
					var newPropertySet = artifactRequest.Artifact.Unpack<PropertySet>();
					if (!ModelManager.CheckForUniqueArtifact(ArtifactType.PropertySet, newPropertySet.Artifact))
					{
						newPropertySet.Artifact = ModelManager.MakeUniqueArtifact(newPropertySet.Artifact);
					}
					artifactName = newPropertySet.Artifact.Name.ToLower();
					outputFolder = GetArtifactFolder(artifactType, artifactName);
					if(newPropertySet.Artifact.ArtifactFiles.Count > 0)
						CreateArtifactFiles(newPropertySet.Artifact.ArtifactFiles, outputFolder, artifactName);
					else
					{
						AddArtifactFiles(outputFolder, artifactName, "PropertySets", artifactType);
					}
					retVal.ArtifactTypeObject= Any.Pack(newPropertySet);
					artifactJson = jsf.Format(newPropertySet);
					break;
				case ArtifactType.TokenTemplate:
					var newTokenTemplate = artifactRequest.Artifact.Unpack<TokenTemplate>();
					if (!ModelManager.CheckForUniqueTemplate(newTokenTemplate.Artifact.Name, newTokenTemplate.Artifact.ArtifactSymbol.Tooling))
					{
						var (newName, artifactSymbol) = ModelManager.MakeUniqueTokenFormula(newTokenTemplate.Artifact.Name, newTokenTemplate.Artifact.ArtifactSymbol);
						newTokenTemplate.Artifact.Name = newName;
						newTokenTemplate.Artifact.ArtifactSymbol = artifactSymbol;
					}
					artifactName = Utils.FirstToUpper(newTokenTemplate.Artifact.Name);
					newTokenTemplate.Artifact.Name = artifactName;
					
					outputFolder = GetArtifactFolder(artifactType, artifactName);
					if(newTokenTemplate.Artifact.ArtifactFiles.Count > 0)
						CreateArtifactFiles(newTokenTemplate.Artifact.ArtifactFiles, outputFolder, artifactName);
					else
					{
						AddArtifactFiles(outputFolder, artifactName, "TokenTemplates", artifactType);
					}
					retVal.ArtifactTypeObject= Any.Pack(newTokenTemplate);
					artifactJson = jsf.Format(newTokenTemplate);
					break;
				default:
					_log.Error("No matching type found for: " + artifactType);
					throw new ArgumentOutOfRangeException();
			}

			_log.Info("Artifact Destination: " + _artifactPath + _folderSeparator + artifactRequest.Type + " folder");
			var formattedJson = JToken.Parse(artifactJson).ToString();
			
			//test to make sure formattedJson will Deserialize.
			try
			{
				switch (artifactRequest.Type)
				{
					case ArtifactType.Base:
						var testBase = JsonParser.Default.Parse<Base>(formattedJson);
						_log.Info("Artifact type: " + artifactType + " successfully deserialized: " +
						          testBase.Artifact.Name);
						break;
					case ArtifactType.Behavior:
						var testBehavior = JsonParser.Default.Parse<Behavior>(formattedJson);
						_log.Info("Artifact type: " + artifactType + " successfully deserialized: " +
						          testBehavior.Artifact.Name);
						break;
					case ArtifactType.BehaviorGroup:
						var testBehaviorGroup = JsonParser.Default.Parse<BehaviorGroup>(formattedJson);
						_log.Info("Artifact type: " + artifactType + " successfully deserialized: " +
						          testBehaviorGroup.Artifact.Name);
						break;
					case ArtifactType.PropertySet:
						var testPropertySet = JsonParser.Default.Parse<PropertySet>(formattedJson);
						_log.Info("Artifact type: " + artifactType + " successfully deserialized: " +
						          testPropertySet.Artifact.Name);
						break;
					case ArtifactType.TokenTemplate:
						var testTemplate = JsonParser.Default.Parse<TokenTemplate>(formattedJson);
						_log.Info("Artifact type: " + artifactType + " successfully deserialized: " +
						          testTemplate.Artifact.Name);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			catch (Exception e)
			{
				_log.Error("Json failed to deserialize back into: " + artifactType);
				_log.Error(e);
				return new NewArtifactResponse();
			}

			_log.Info("Creating Artifact: " + formattedJson);
			if (outputFolder != null)
			{
				var artifactStream = File.CreateText(outputFolder.FullName + _folderSeparator + artifactName + ".json");
				artifactStream.Write(formattedJson);
				artifactStream.Close();
			}
			
			_log.Info("Complete");
			return retVal;
		}
		
		public static UpdateArtifactResponse UpdateArtifact(UpdateArtifactRequest artifactRequest)
		{
			string artifactJson;
			
			var jsf = new JsonFormatter(new JsonFormatter.Settings(true));
			var artifactType = artifactRequest.Type;
	
			var retVal = new UpdateArtifactResponse
			{
				Type = artifactType
			};

			string artifactName;
			string existingVersion;
			switch (artifactType)
			{
				case ArtifactType.Base:
					_log.Info("UpdateArtifact was requested to update a base token type, which is not supported.");
					break;
				case ArtifactType.Behavior:
					var updateBehavior = artifactRequest.ArtifactTypeObject.Unpack<Behavior>();

					var existingBehavior = ModelManager.GetBehaviorArtifact(new ArtifactSymbol
					{
						Tooling = updateBehavior.Artifact.ArtifactSymbol.Tooling
					});
					existingVersion = existingBehavior.Artifact.ArtifactSymbol.Version;
					existingBehavior.MergeFrom(updateBehavior);
					artifactName = updateBehavior.Artifact.Name.ToLower();
					artifactJson = jsf.Format(existingBehavior);
					var (outcomeB, messageB) = VersionArtifact(BehaviorFolder, artifactName,
						existingVersion, artifactJson, artifactType);
					if (outcomeB)
					{
						ModelManager.AddOrUpdateInMemoryArtifact(artifactRequest.Type,
							Any.Pack(existingBehavior));
						retVal.ArtifactTypeObject = Any.Pack(existingBehavior);
						retVal.Updated = true;
					}
					else
					{
						retVal.Updated = false;
						_log.Error(messageB);
					}
					_log.Info("TOM and Artifact updated.");
					return retVal;
				case ArtifactType.BehaviorGroup:
					var updateBehaviorGroup = artifactRequest.ArtifactTypeObject.Unpack<BehaviorGroup>();

					var existingBehaviorGroup = ModelManager.GetBehaviorGroupArtifact(new ArtifactSymbol
					{
						Tooling = updateBehaviorGroup.Artifact.ArtifactSymbol.Tooling
					});
					existingVersion = existingBehaviorGroup.Artifact.ArtifactSymbol.Version;
					existingBehaviorGroup.MergeFrom(updateBehaviorGroup);
					artifactName = updateBehaviorGroup.Artifact.Name.ToLower();

					artifactJson = jsf.Format(existingBehaviorGroup);
					var (outcomeBg, messageBg) = VersionArtifact(BehaviorGroupFolder, artifactName,
						existingVersion, artifactJson, artifactType);
					if (outcomeBg)
					{
						ModelManager.AddOrUpdateInMemoryArtifact(artifactRequest.Type,
							Any.Pack(existingBehaviorGroup));
						retVal.ArtifactTypeObject = Any.Pack(existingBehaviorGroup);
						retVal.Updated = true;
					}
					else
					{
						retVal.Updated = false;
						_log.Error(messageBg);
					}
					_log.Info("TOM and Artifact updated.");
					return retVal;
				case ArtifactType.PropertySet:
					var updatePropertySet = artifactRequest.ArtifactTypeObject.Unpack<PropertySet>();

					var existingPropertySet = ModelManager.GetPropertySetArtifact(new ArtifactSymbol
					{
						Tooling = updatePropertySet.Artifact.ArtifactSymbol.Tooling
					});
					existingVersion = existingPropertySet.Artifact.ArtifactSymbol.Version;
					existingPropertySet.MergeFrom(updatePropertySet);
					artifactName = updatePropertySet.Artifact.Name.ToLower();
					
					artifactJson = jsf.Format(existingPropertySet);
					var (outcomePs, messagePs) = VersionArtifact(PropertySetFolder, artifactName,
						existingVersion, artifactJson, artifactType);
					if (outcomePs)
					{
						ModelManager.AddOrUpdateInMemoryArtifact(artifactRequest.Type,
							Any.Pack(existingPropertySet));
						retVal.ArtifactTypeObject = Any.Pack(existingPropertySet);
						retVal.Updated = true;
					}
					else
					{
						retVal.Updated = false;
						_log.Error(messagePs);
					}
					_log.Info("TOM and Artifact updated.");
					return retVal;
				case ArtifactType.TokenTemplate:
					var updateTokenTemplate = artifactRequest.ArtifactTypeObject.Unpack<TokenTemplate>();

					var existingTokenTemplate =
						ModelManager.GetTokenTemplateArtifact(updateTokenTemplate.Artifact.ArtifactSymbol);
					existingVersion = existingTokenTemplate.Artifact.ArtifactSymbol.Version;
					existingTokenTemplate.MergeFrom(updateTokenTemplate);
					existingTokenTemplate.Artifact.ArtifactSymbol.Id = Guid.NewGuid().ToString();
					artifactName = updateTokenTemplate.Artifact.Name.ToLower();
					
					artifactJson = jsf.Format(existingTokenTemplate);
					var (outcomeT, messageT) = VersionArtifact(TokenTemplatesFolder, artifactName,
						existingVersion, artifactJson, artifactType);
					if (outcomeT)
					{
						ModelManager.AddOrUpdateInMemoryArtifact(artifactRequest.Type,
							Any.Pack(existingTokenTemplate));
						retVal.ArtifactTypeObject = Any.Pack(existingTokenTemplate);
						retVal.Updated = true;
					}
					else
					{
						retVal.Updated = false;
						_log.Error(messageT);
					}
					_log.Info("TOM and Artifact updated.");
					return retVal;
				default:
					_log.Error("No matching type found for: " + artifactType);
					throw new ArgumentOutOfRangeException();
			}

			return retVal;
		}

		private static bool SaveArtifact(ArtifactType type, string artifactName, string artifactJson,
			FileSystemInfo outputFolder)
		{
			_log.Info("Artifact Destination: " + _artifactPath + _folderSeparator + type + " folder");
			var formattedJson = JToken.Parse(artifactJson).ToString();

			//test to make sure formattedJson will Deserialize.
			try
			{
				switch (type)
				{
					case ArtifactType.Base:
						var testBase = JsonParser.Default.Parse<Base>(formattedJson);
						_log.Info("Artifact type: " + type + " successfully deserialized: " +
						          testBase.Artifact.Name);
						break;
					case ArtifactType.Behavior:
						var testBehavior = JsonParser.Default.Parse<Behavior>(formattedJson);
						_log.Info("Artifact type: " + type + " successfully deserialized: " +
						          testBehavior.Artifact.Name);
						break;
					case ArtifactType.BehaviorGroup:
						var testBehaviorGroup = JsonParser.Default.Parse<BehaviorGroup>(formattedJson);
						_log.Info("Artifact type: " + type + " successfully deserialized: " +
						          testBehaviorGroup.Artifact.Name);
						break;
					case ArtifactType.PropertySet:
						var testPropertySet = JsonParser.Default.Parse<PropertySet>(formattedJson);
						_log.Info("Artifact type: " + type + " successfully deserialized: " +
						          testPropertySet.Artifact.Name);
						break;
					case ArtifactType.TokenTemplate:
						var testTemplate = JsonParser.Default.Parse<TokenTemplate>(formattedJson);
						_log.Info("Artifact type: " + type + " successfully deserialized: " +
						          testTemplate.Artifact.Name);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			catch (Exception e)
			{
				_log.Error("Json failed to deserialize back into: " + type);
				_log.Error(e);
				return false;
			}

			_log.Info("Saving Artifact: " + formattedJson);
			if (outputFolder != null)
			{
				var artifactStream = File.CreateText(outputFolder.FullName + _folderSeparator + artifactName + ".json");
				artifactStream.Write(formattedJson);
				artifactStream.Close();
			}

			_log.Info("Complete");
			return true;
		}

		internal static DeleteArtifactResponse DeleteArtifact(DeleteArtifactRequest artifactRequest)
		{
			_log.Info("DeleteArtifact of type: " + artifactRequest.ArtifactSymbol.Type + " with Tooling symbol: " + artifactRequest.ArtifactSymbol);
			DeleteArtifactResponse response;
			var artifactFolderName =
				ModelManager.GetArtifactFolderNameBySymbol(artifactRequest.ArtifactSymbol.Type, artifactRequest.ArtifactSymbol
					.Tooling);
			try
			{
				switch (artifactRequest.ArtifactSymbol.Type)
				{
					case ArtifactType.Base:
						DeleteArtifactFolder(BaseFolder, artifactFolderName);
						break;
					case ArtifactType.Behavior:
						DeleteArtifactFolder(BehaviorFolder, artifactFolderName);
						break;
					case ArtifactType.BehaviorGroup:
						DeleteArtifactFolder(BehaviorGroupFolder, artifactFolderName);
						break;
					case ArtifactType.PropertySet:
						DeleteArtifactFolder(PropertySetFolder, artifactFolderName);
						break;
					case ArtifactType.TokenTemplate:
						DeleteArtifactFolder(TokenTemplatesFolder, artifactFolderName);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				response = new DeleteArtifactResponse
				{
					Deleted = true
				};
			}
			catch (Exception e)
			{
				_log.Error("Error attempting to delete artifact of type: " + artifactRequest.ArtifactSymbol.Type + " with Tooling symbol: " + artifactRequest.ArtifactSymbol);
				_log.Error(e);
				response = new DeleteArtifactResponse
				{
					Deleted = false
				};
			}

			return response;
		}
		
		#endregion
		
		#region Artifact Utils

		private static DirectoryInfo GetArtifactFolder(ArtifactType type, string artifactName)
		{
			string typeFolderName;
			switch (type)
			{
				case ArtifactType.Base:
					typeFolderName = BaseFolder;
					break;
				case ArtifactType.Behavior:
					typeFolderName = BehaviorFolder;
					break;
				case ArtifactType.BehaviorGroup:
					typeFolderName = BehaviorGroupFolder;
					break;
				case ArtifactType.PropertySet:
					typeFolderName = PropertySetFolder;
					break;
				case ArtifactType.TokenTemplate:
					typeFolderName = TokenTemplatesFolder;
				case ArtifactType.TokenDefinition:
					typeFolderName = TokenDefinitionsFolder;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}

			var path = _artifactPath + _folderSeparator + typeFolderName + _folderSeparator + artifactName + _latest;
			return Directory.Exists(path) ? new DirectoryInfo(_artifactPath + _folderSeparator + typeFolderName + _folderSeparator + artifactName + _latest) : Directory.CreateDirectory(path);
		}
		
		private static void DeleteArtifactFolder(string artifactTypeFolderName, string artifactFolderName)
		{
			try
			{
				Directory.Delete(
					_artifactPath + _folderSeparator + artifactTypeFolderName + _folderSeparator +
					artifactFolderName, true);
			} 
			catch (Exception e) 
			{
				_log.Error("Unable to Delete Artifact Folder: " + artifactTypeFolderName + _folderSeparator + artifactFolderName);
				_log.Error(e);
			} 
		}
		
		private static void CreateArtifactFiles(IEnumerable<ArtifactFile> artifactArtifactFiles, FileSystemInfo outputFolder, string artifactName)
		{
			foreach (var af in artifactArtifactFiles)
			{
				switch (af.Content)
				{
					case ArtifactContent.Uml:
					{
						_log.Info("Creating Artifact MD UML File");
						var md  = File.CreateText(outputFolder.FullName + _folderSeparator + artifactName+".md");
						md.Write(af.FileData.ToStringUtf8());
						md.Close();
						break;
					}
					case ArtifactContent.Other:
					{
						_log.Info("Creating Artifact Other File");
						var other  = File.Create(outputFolder.FullName + _folderSeparator + af.FileName);
						other.Write(af.FileData.ToByteArray());
						other.Close();
						break;
					}
					case ArtifactContent.Definition:
						break;
					case ArtifactContent.Control:
						_log.Info("Creating Artifact Proto Control");
						var proto  = File.CreateText(outputFolder.FullName + _folderSeparator + artifactName+".proto");
						proto.Write(af.FileData.ToStringUtf8());
						proto.Close();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private static Artifact CreateGenericArtifactObject(string name, ArtifactType artifactType)
		{
			var artifact =  new Artifact
			{
				Name = name,
				ArtifactSymbol = new ArtifactSymbol
				{
					Id = Guid.NewGuid().ToString(),
					Type = artifactType,
					Tooling = "",
					Visual = "",
					Version = "1.0",
					Validated = false
				},
				ArtifactDefinition = new ArtifactDefinition
				{
					BusinessDescription = "This is a " + name + " of type: " + artifactType,
					BusinessExample = "",
					Comments = "",
					Analogies = { new ArtifactAnalogy
					{
						Name = "Analogy 1",
						Description = name + " analogy 1 description"
					}}
				},
				Maps = new Maps
				{
					CodeReferences = { new MapReference
					{
						MappingType = MappingType.SourceCode,
						Name = "Code 1",
						Platform = TargetPlatform.Daml,
						ReferencePath = ""
					}},
					ImplementationReferences = { new MapReference
					{
						MappingType = MappingType.Implementation,
						Name = "Implementation 1",
						Platform = TargetPlatform.ChaincodeGo,
						ReferencePath = ""
					}},
					Resources = { new MapResourceReference
					{
						MappingType = MappingType.Resource,
						Name = "Regulation Reference 1",
						Description = "",
						ResourcePath = ""
					}}
				},
				IncompatibleWithSymbols = { new ArtifactSymbol
				{
					Type = artifactType,
					Tooling = "",
					Visual = ""
				}},
				Dependencies = { new SymbolDependency
				{
					Description = "",
					Symbol = new ArtifactSymbol()
				}},
				Aliases = { "alias1", "alias2"}
				
			};

			return artifact;
		}

		private static FormulaGrammar GenerateFormula()
		{
			var formula = new FormulaGrammar();

			var singleToken = new SingleToken
			{
				BaseToken = new TokenBase
				{
					ArtifactSymbol = new ArtifactSymbol()
				},
				Behaviors = new BehaviorList
				{
					ListStart = "{",
					BehaviorSymbols = { new ArtifactSymbol
					{
						Type = ArtifactType.Behavior
					}},
					ListEnd = "}"
				},
				GroupStart = "[",
				GroupEnd = "]"
			};
			
			var psli = new PropertySetListItem
			{
				ListStart = "+",
				PropertySetSymbols = new ArtifactSymbol
				{
					Type = ArtifactType.PropertySet
				}
			};
			singleToken.PropertySets.Add(psli);

			formula.SingleToken = singleToken;

			var hybrid = new HybridTokenFormula
			{
				ChildrenStart = "(",
				ChildrenEnd = ")",
				Parent = singleToken
			};
			hybrid.ChildTokens.Add(singleToken);
			hybrid.ChildTokens.Add(singleToken);

			formula.Hybrid = hybrid;
			
			var hybridHybrids = new HybridTokenWithHybridChildrenFormula
			{
				HybridChildrenStart = "(",
				HybridChildrenEnd = ")",
				Parent = singleToken
			};
			hybridHybrids.HybridChildTokens.Add(hybrid);
			hybridHybrids.HybridChildTokens.Add(hybrid);
			formula.HybridWithHybrids = hybridHybrids;
			
			
			return formula;
		}
		
		
		private static Base GetTokenTypeBase(string fullPath, TokenType tokenType)
		{
			string baseName;
			const string typeFolder = "base";
			
			switch (tokenType)
			{
				case TokenType.Fungible:
					baseName = "fungible";
					break;
				case TokenType.NonFungible:
					baseName = "non-fungible";
					break;
				case TokenType.HybridFungibleRoot:
					baseName = "hybrid-fungibleRoot";
					break;
				case TokenType.HybridNonFungibleRoot:
					baseName = "hybrid-non-fungibleRoot";
					break;
				case TokenType.HybridFungibleRootHybridChildren:
					baseName = "hybrid-non-fungibleRoot-hybridChildren";
					break;
				case TokenType.HybridNonFungibleRootHybridChildren:
					baseName = "hybrid-non-fungibleRoot-hybridChildren";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			var baseFile = File.OpenText(fullPath + typeFolder + _folderSeparator + baseName + _folderSeparator + TxService.Latest + _folderSeparator + baseName+".json");
			var json = baseFile.ReadToEnd();
			var formattedJson = JToken.Parse(json).ToString();
			var baseType = JsonParser.Default.Parse<Base>(formattedJson);
			return baseType;
		}


		private static void AddTemplateFiles(DirectoryInfo outputFolder, string typeFolder, string templateName, string nameSpaceAdd, string classification)
		{
			CreateMarkdown(outputFolder, templateName, ArtifactType.TokenTemplate);
			CreateProto(outputFolder, templateName, nameSpaceAdd);
		}

		private static Artifact GetArtifactFiles(DirectoryInfo ad, Artifact artifact)
		{
			foreach (var af in ad.EnumerateFiles())
			{
				if (af.Name.EndsWith("proto"))
				{
					var protoFile = GetArtifactText(af);
					artifact.ControlUri = af.Name;
					artifact.ArtifactFiles.Add(new ArtifactFile
					{
						FileName = af.Name,
						FileData = ByteString.CopyFromUtf8(protoFile),
						Content = ArtifactContent.Control
					});
					continue;
				}

				if (af.Name.EndsWith("md"))
				{
					var mdFile = GetArtifactText(af);

					artifact.ArtifactFiles.Add(new ArtifactFile
					{
						FileName = af.Name,
						FileData = ByteString.CopyFromUtf8(mdFile),
						Content = ArtifactContent.Uml
					});
					continue;
				}

				if (af.Name.EndsWith("json"))
				{
					continue;
				}

				var otherFile = GetArtifactBytes(af);

				var other = new ArtifactFile
				{
					FileName = af.Name,
					Content = ArtifactContent.Other
				};
				if (!af.Name.EndsWith(".DS_Store"))
					other.FileData = ByteString.CopyFrom(otherFile);
				artifact.ArtifactFiles.Add(other);
			}

			return artifact;
		}

		private static (string,IEnumerable<ArtifactFile>) GetTemplateFiles(DirectoryInfo ad)
		{
			var retVal = new List<ArtifactFile>();
			var uri = "";
			foreach (var af in ad.EnumerateFiles())
			{
				if (af.Name.EndsWith("proto"))
				{
					var protoFile = GetArtifactText(af);
					uri = af.Name;
					retVal.Add(new ArtifactFile
					{
						FileName = af.Name,
						FileData = ByteString.CopyFromUtf8(protoFile),
						Content = ArtifactContent.Control
					});
					continue;
				}

				if (af.Name.EndsWith("md"))
				{
					var mdFile = GetArtifactText(af);

					retVal.Add(new ArtifactFile
					{
						FileName = af.Name,
						FileData = ByteString.CopyFromUtf8(mdFile),
						Content = ArtifactContent.Uml
					});
					continue;
				}

				if (af.Name.EndsWith("json"))
				{
					continue;
				}

				var otherFile = GetArtifactBytes(af);

				var other = new ArtifactFile
				{
					FileName = af.Name,
					Content = ArtifactContent.Other
				};
				if (!af.Name.EndsWith(".DS_Store"))
					other.FileData = ByteString.CopyFrom(otherFile);
				retVal.Add(other);
			}

			return (uri, retVal);
		}
		
		private static T GetArtifact<T>(FileInfo artifact) where T : IMessage, new()
		{
			var typeFile = artifact.OpenText();
			var json = typeFile.ReadToEnd();
			var formattedJson = JToken.Parse(json).ToString();
			return JsonParser.Default.Parse<T>(formattedJson);
		}

		private static string GetArtifactText(FileInfo artifactFile)
		{
			var typeFile = artifactFile.OpenText();
			return typeFile.ReadToEnd();
		}

		private static byte[] GetArtifactBytes(FileInfo artifactFile)
		{
			using (var ms = new MemoryStream())
			{
				var f = artifactFile.OpenRead();
				f.CopyTo(ms);
				return  ms.ToArray();
			}
		}

		internal static TokenTemplate GetTokenTemplateTree(TokenTemplate template)
		{
			var retVal = template.Clone();
			var (baseToken, behaviors, behaviorGroups, propertySets) = GetTokenComponents(template);

			retVal.Parent.Base = baseToken;
			foreach (var b in behaviors)
			{
				var behavior = retVal.Parent.Behaviors.SingleOrDefault(e =>
					e.Symbol.Tooling == b.Artifact.ArtifactSymbol.Tooling);
				if (behavior != null) behavior.Behavior = b;
			}
			foreach (var b in behaviorGroups)
			{
				var behaviorGroup = retVal.Parent.BehaviorGroups.SingleOrDefault(e =>
					e.Symbol.Tooling == b.Artifact.ArtifactSymbol.Tooling);
				if (behaviorGroup != null) behaviorGroup.BehaviorGroup = b;
			}
			foreach (var p in propertySets)
			{
				var propertySet = retVal.Parent.PropertySets.SingleOrDefault(e =>
					e.Symbol.Tooling == p.Artifact.ArtifactSymbol.Tooling);
				if (propertySet != null) propertySet.PropertySet = p;
			}
			
			//iterate through any children
			foreach (var child in retVal.ChildTokens){
				var (childToken, childBehaviors, childBehaviorGroups, childPropertySets) = GetTokenComponents(template);

				child.Base = childToken;
				foreach (var b in childBehaviors)
				{
					var behavior = child.Behaviors.SingleOrDefault(e =>
						e.Symbol.Tooling == b.Artifact.ArtifactSymbol.Tooling);
					if (behavior != null) behavior.Behavior = b;
				}
				foreach (var b in childBehaviorGroups)
				{
					var behaviorGroup = child.BehaviorGroups.SingleOrDefault(e =>
						e.Symbol.Tooling == b.Artifact.ArtifactSymbol.Tooling);
					if (behaviorGroup != null) behaviorGroup.BehaviorGroup = b;
				}
				foreach (var p in childPropertySets)
				{
					var propertySet = child.PropertySets.SingleOrDefault(e =>
						e.Symbol.Tooling == p.Artifact.ArtifactSymbol.Tooling);
					if (propertySet != null) propertySet.PropertySet = p;
				}
			}
			
			return retVal;
		}

		private static (Base, IEnumerable<Behavior>, IEnumerable<BehaviorGroup>, IEnumerable<PropertySet>) GetTokenComponents(TokenTemplate template)
		{

			var baseToken = ModelManager.GetBaseArtifact(template.Parent.Formula);

			var behaviorList = template.Parent.Behaviors.Select(t => ModelManager.GetBehaviorArtifact(t.Symbol)).ToList();
			var behaviorGroupList = template.Parent.BehaviorGroups.Select(t => ModelManager.GetBehaviorGroupArtifact(t.Symbol)).ToList();
			var propertySetList = template.Parent.PropertySets.Select(t => ModelManager.GetPropertySetArtifact(t.Symbol)).ToList();

			return (baseToken, behaviorList, behaviorGroupList, propertySetList);
		}

		private static (bool, string) VersionArtifact(string artifactTypeFolder, string artifactName, string version,
				string artifactJson, ArtifactType artifactType)
		{
			try
			{
				var latestPath = _artifactPath + _folderSeparator + artifactTypeFolder + _folderSeparator +
				                 artifactName + _latest;

				if (string.IsNullOrEmpty(version))
					version = "1.0";
				var oldLatestPath = _artifactPath + _folderSeparator + artifactTypeFolder + _folderSeparator +
				                    artifactName + _folderSeparator + version;
				var (outcome, message) = Createversion(latestPath, oldLatestPath);
				if (!outcome) return (false, message);
				var outputFolder =
					new DirectoryInfo(latestPath);

				return (SaveArtifact(artifactType, artifactName, artifactJson, outputFolder), "latest");
			}
			catch (Exception e)
			{
				_log.Error(e);
				return (false, e.Message);
			}
		}
		
		private static (bool, string) Createversion(string sourceDirName, string destDirName)
		{
			// Get the subdirectories for the specified directory.
			var dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				var err = "New Version could not be created, source directory does not exist or could not be found: "
					+ sourceDirName;
				_log.Error(err);
				return (false, err);
			}

			
			// If the destination directory doesn't exist, create it.
			if (Directory.Exists(destDirName))
			{
				_log.Error(destDirName + " already exists, creating a backup.");
				destDirName = Utils.Randomize(destDirName);
			}
			if (!Directory.Exists(destDirName))
			{

				Directory.CreateDirectory(destDirName);
				_log.Error(destDirName + " is previous Version.");
			}
        
			// Get the files in the directory and copy them to the new location.
			var files = dir.GetFiles();
			foreach (var file in files)
			{
				var tempPath = Path.Combine(destDirName, file.Name);
				file.CopyTo(tempPath, false);
			}

			return (true, "");
		}
		
		private static void AddArtifactFiles(DirectoryInfo outputFolder, string artifactName, string nameSpaceAdd, ArtifactType artifactType)
		{
			try
			{
				CreateMarkdown(outputFolder, artifactName, artifactType);
				CreateProto(outputFolder, artifactName, nameSpaceAdd);
			}
			catch (Exception e)
			{
				_log.Error("Error creating new artifact files");
				_log.Error(e);
			}
		}
		private static void CreateMarkdown(DirectoryInfo outputFolder, string artifactName, ArtifactType artifactType)
		{
			_log.Info("Creating Artifact Markdown");
			var md = File.CreateText(outputFolder + _folderSeparator + artifactName + ".md");
			md.Write("# " + artifactName + " a TTF " + artifactType);
			md.Close();

		}
		
		private static void CreateProto(DirectoryInfo outputFolder, string artifactName, string nameSpaceAdd)
		{
			_log.Info("Creating Artifact Proto");
			var pFile = outputFolder + _folderSeparator + artifactName + ".proto";
			
			var proto  = File.CreateText(pFile);
			var templateProto =
				File.ReadAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + 
				                 _folderSeparator + "templates" + _folderSeparator + "artifact.proto");
			
			var ns = templateProto.Replace("BASE", nameSpaceAdd);
			ns = ns.Replace("NAME", artifactName);
			ns = ns.Replace("bASE", nameSpaceAdd.ToLower());
			proto.Write(ns.Replace("ARTIFACT", artifactName));
			proto.Close();
		}
		#endregion
	}
}