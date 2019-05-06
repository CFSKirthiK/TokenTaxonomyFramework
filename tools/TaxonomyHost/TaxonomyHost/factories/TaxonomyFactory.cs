using System;
using System.IO;
using System.Reflection;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json.Linq;
using TTF.Tokens.Model.Artifact;
using TTF.Tokens.Model.Core;
using TTI.TTF.Taxonomy.Model;

namespace TaxonomyHost.factories
{
	public static class TaxonomyFactory
	{
		private const string BaseFolder = "base";
		private const string BehaviorFolder = "behaviors";
		private const string BehaviorGroupFolder = "behavior-groups";
		private const string PropertySetFolder = "property-sets";
		private const string TokenTemplates = "token-templates";
		private static readonly string _artifactPath;
		private static readonly string _folderSeparator = TaxonomyService.FolderSeparator;
		private static readonly ILog _log;

		static TaxonomyFactory()
		{
			Utils.InitLog();
			_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			_artifactPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + _folderSeparator +
			                TaxonomyService.ArtifactPath;
		}

		#region load
		internal static Taxonomy Load()
		{
			if (!Directory.Exists(_artifactPath))
			{
				var err = "Artifact Path not found: " + _artifactPath;
				_log.Error(err);
				throw new Exception(err);
			}

			_log.Info("Artifact Folder Found, loading to Taxonomy.");
			var root = new DirectoryInfo(_artifactPath);
			Taxonomy taxonomy;
			var rJson = root.GetFiles("*.json");
			try
			{
				taxonomy = GetArtifact<Taxonomy>(rJson[0]);
				_log.Info("Loaded Taxonomy Version: " + taxonomy.Version);
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
					var bJson = ad.GetFiles("*.json");
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

					baseType.Artifact = GetArtifactFiles(ad, baseType.Artifact);
					taxonomy.BaseTokenTypes.Add(baseType.Artifact.ArtifactSymbol.ToolingSymbol, baseType);
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
					var bJson = ad.GetFiles("*.json");
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
					
					behavior.Artifact = GetArtifactFiles(ad, behavior.Artifact);
					taxonomy.Behaviors.Add(behavior.Artifact.ArtifactSymbol.ToolingSymbol, behavior);
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
					var bJson = ad.GetFiles("*.json");
					try
					{
						behaviorGroup = GetArtifact<BehaviorGroup>(bJson[0]);
						
					}
					catch (Exception e)
					{
						_log.Error("Failed to load BehaviorGroup: " +ad.Name);
						_log.Error(e);
						continue;
					}

					behaviorGroup.Artifact = GetArtifactFiles(ad, behaviorGroup.Artifact);
					taxonomy.BehaviorGroups.Add(behaviorGroup.Artifact.ArtifactSymbol.ToolingSymbol, behaviorGroup);
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
					var bJson = ad.GetFiles("*.json");
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

					propertySet.Artifact = GetArtifactFiles(ad, propertySet.Artifact);
					taxonomy.PropertySets.Add(propertySet.Artifact.ArtifactSymbol.ToolingSymbol, propertySet);
				}
			}

			if (!Directory.Exists(aPath + TokenTemplates)) return taxonomy;
			{
				_log.Info("TokenTemplate Artifact Folder Found, loading to TokenTemplates");
				var tokenTemplates = new DirectoryInfo(aPath + TokenTemplates);
				foreach (var ad in tokenTemplates.EnumerateDirectories())
				{
					TokenTemplate tokenTemplate;
					_log.Info("Loading " + ad.Name);
					var bJson = ad.GetFiles("*.json");
					try
					{
						tokenTemplate = GetArtifact<TokenTemplate>(bJson[0]);
						
					}
					catch (Exception e)
					{
						_log.Error("Failed to load TokenTemplate: " + ad.Name);
						_log.Error(e);
						continue;
					}

					tokenTemplate.Artifact = GetArtifactFiles(ad, tokenTemplate.Artifact);
					taxonomy.TokenTemplates.Add(tokenTemplate.Artifact.ArtifactSymbol.ToolingSymbol, tokenTemplate);
				}
			}

			return taxonomy;
		}

		#endregion
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
				}
				else
				{
					var otherFile = GetArtifactBytes(af);

					artifact.ArtifactFiles.Add(new ArtifactFile
					{
						FileName = af.Name,
						FileData = ByteString.CopyFrom(otherFile),
						Content = ArtifactContent.Other
					});
				}
			}

			return artifact;
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
			
		}

	}
}