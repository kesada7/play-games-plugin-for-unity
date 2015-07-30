// <copyright file="GPGSAndroidSetupUI.cs" company="Google Inc.">
// Copyright (C) Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>

namespace GooglePlayGames
{
    using System.IO;
    using System.Collections;
    using System.Xml;
    using UnityEngine;
    using UnityEditor;

    public class GPGSAndroidSetupUI : EditorWindow
    {
        private string mConfigData = string.Empty;
        private string mClassName = "GooglePlayGames.GPGSIds";
        private Vector2 scroll;

        private string mClientId = string.Empty;

        [MenuItem("Window/Google Play Games/Setup/Android setup...", false, 1)]
        public static void MenuItemFileGPGSAndroidSetup()
        {
            EditorWindow window = EditorWindow.GetWindow(
                typeof(GPGSAndroidSetupUI), true, GPGSStrings.AndroidSetup.Title);
            window.minSize = new Vector2(500, 400);
        }

        public void OnEnable()
        {
            mClassName = GPGSProjectSettings.Instance.Get("proj.ConstantsClassName");
            mConfigData = GPGSProjectSettings.Instance.Get("proj.ResourceData");
            mClientId = GPGSProjectSettings.Instance.Get(GPGSUtil.ANDROIDCLIENTIDKEY);
        }

        public void OnGUI()
        {
            GUI.skin.label.wordWrap = true;
            GUILayout.BeginVertical();

            GUILayout.Space(10);
            GUILayout.Label(GPGSStrings.AndroidSetup.Blurb);
           
            GUILayout.Label("Constants class name", EditorStyles.boldLabel);
            GUILayout.Label("Enter the fully qualified name of the class to create containing the constants");
            GUILayout.Space(10);

            mClassName = EditorGUILayout.TextField("Constants class name",
                mClassName,GUILayout.Width(480));
            
            GUILayout.Label("Resources Definition", EditorStyles.boldLabel);
            GUILayout.Label("Paste in the Android Resources from the Play Console");
            GUILayout.Space(10);
            scroll = GUILayout.BeginScrollView(scroll);
            mConfigData = EditorGUILayout.TextArea(mConfigData,
                GUILayout.Width(475), GUILayout.Height(Screen.height));
            GUILayout.EndScrollView();
            GUILayout.Space(10);

            // Client ID field
            GUILayout.Label(GPGSStrings.Setup.ClientIdTitle, EditorStyles.boldLabel);
            GUILayout.Label(GPGSStrings.AndroidSetup.ClientIdBlurb);
      
            mClientId = EditorGUILayout.TextField(GPGSStrings.Setup.ClientId,
                mClientId, GUILayout.Width(450));

            GUILayout.Space(10);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(GPGSStrings.Setup.SetupButton,
                GUILayout.Width(100)))
            {
                DoSetup();
            }

            if (GUILayout.Button("Cancel",GUILayout.Width(100)))
            {
                this.Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.EndVertical();
        }

        public void DoSetup()
        {
            if (PerformSetup(mClientId, mClassName, mConfigData, null))
            {
                EditorUtility.DisplayDialog(GPGSStrings.Success,
                    GPGSStrings.AndroidSetup.SetupComplete, GPGSStrings.Ok);
                this.Close();
            }
            else
            {
                GPGSUtil.Alert(GPGSStrings.Error,
                    "Invalid or missing XML resource data.  Make sure the data is" +
                    " valid and contains the app_id element");
            }
           
        }

        private static bool ParseResources(string className, string res)
        {
            XmlTextReader reader = new XmlTextReader(new StringReader(res));
            bool inResource = false;
            string lastProp = null;
            Hashtable resourceKeys = new Hashtable();
            string appId = null;
            while (reader.Read())
            {
                if (reader.Name == "resources")
                {
                    inResource = true;
                }
                if (inResource && reader.Name == "string")
                {
                    lastProp = reader.GetAttribute("name");
                }
                else if (inResource && !string.IsNullOrEmpty(lastProp))
                {
                    if (reader.HasValue)
                    {
                        if (lastProp == "app_id")
                        {
                            appId = reader.Value;
                            GPGSProjectSettings.Instance.Set("proj.AppId", appId);
                        }
                        else
                        {
                            resourceKeys[lastProp] = reader.Value;
                        }
                        lastProp = null;
                    }
                }
            }
            reader.Close();
            if (resourceKeys.Count > 0)
            {
                GPGSUtil.WriteResourceIds(className, resourceKeys);
            }
            return appId != null;
        }


        /// <summary>
        /// Performs setup using the Android resources downloaded XML file
        /// from the play console.
        /// </summary>
        /// <returns><c>true</c>, if setup was performed, <c>false</c> otherwise.</returns>
        /// <param name="className">Fully qualified class name for the resource Ids.</param>
        /// <param name="resourceXmlData">Resource xml data.</param>
        /// <param name="nearbySvcId">Nearby svc identifier.</param>
        public static bool PerformSetup(string clientId, string className, string resourceXmlData, string nearbySvcId)
        {
            if (ParseResources(className, resourceXmlData))
            {
                GPGSProjectSettings.Instance.Set("proj.ConstantsClassName", className);
                GPGSProjectSettings.Instance.Set("proj.ResourceData", resourceXmlData);
                return PerformSetup(clientId, GPGSProjectSettings.Instance.Get("proj.AppId"), nearbySvcId);
            }
            return false;
        }

        /// <summary>
        /// Provide static access to setup for facilitating automated builds.
        /// </summary>
        /// <param name="appId">App identifier.</param>
        /// <param name="nearbySvcId">Optional nearby connection serviceId</param>
        public static bool PerformSetup(string clientId, string appId, string nearbySvcId)
        {
            if( !string.IsNullOrEmpty(clientId) )
            {
                if (!GPGSUtil.LooksLikeValidClientId(clientId))
                {
                    GPGSUtil.Alert(GPGSStrings.Setup.ClientIdError);
                    return false;
                }
                appId = clientId.Split('-')[0];
            }
            else
            {
                // check for valid app id
                if (!GPGSUtil.LooksLikeValidAppId(appId))
                {
                    GPGSUtil.Alert(GPGSStrings.Setup.AppIdError);
                    return false;
                }
            }

            if (nearbySvcId != null)
            {
                if (!NearbyConnectionUI.PerformSetup(nearbySvcId, true))
                {
                    return false;
                }
            }

            GPGSProjectSettings.Instance.Set("proj.AppId", appId);
            GPGSProjectSettings.Instance.Set(GPGSUtil.ANDROIDCLIENTIDKEY, clientId);
            GPGSProjectSettings.Instance.Save();
            GPGSUtil.UpdateGameInfo();

            // check that Android SDK is there
            if (!GPGSUtil.HasAndroidSdk())
            {
                Debug.LogError("Android SDK not found.");
                EditorUtility.DisplayDialog(GPGSStrings.AndroidSetup.SdkNotFound,
                    GPGSStrings.AndroidSetup.SdkNotFoundBlurb, GPGSStrings.Ok);
                return false;
            }

            GPGSUtil.CopySupportLibs();

            // Generate AndroidManifest.xml
            GPGSUtil.GenerateAndroidManifest();
            
            FillInAppData(GameInfoPath, GameInfoPath, clientId);

            // refresh assets, and we're done
            AssetDatabase.Refresh();
            GPGSProjectSettings.Instance.Set("android.SetupDone", true);
            GPGSProjectSettings.Instance.Save();

            return true;
        }
        
        /// <summary>
        /// Helper function to do search and replace of the client and bundle ids.
        /// </summary>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="outputPath">Output path.</param>
        /// <param name="clientId">Client identifier.</param>
        /// <param name="bundleId">Bundle identifier.</param>
        private static void FillInAppData(string sourcePath,
                                          string outputPath,
                                          string clientId)
        {
            string fileBody = GPGSUtil.ReadFully(sourcePath);
            fileBody = fileBody.Replace(GPGSUtil.ANDROIDCLIENTIDPLACEHOLDER, clientId);
            GPGSUtil.WriteFile(outputPath, fileBody);
        }
    }
}
