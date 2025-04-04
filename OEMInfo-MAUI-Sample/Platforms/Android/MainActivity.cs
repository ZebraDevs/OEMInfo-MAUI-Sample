using Android.App;
using Android.Content.PM;
using Android.Database;
using Android.Nfc;
using Android.OS;
using Android.Util;
using Android.Widget;

using System;
using System.Data;
using Android.Database;
using Android.Net;
using Android.Util;
using Android.Widget;
using CommunityToolkit.Mvvm.Messaging;
using Symbol.XamarinEMDK;

using System.Xml;
using System.Text;


namespace OEMInfo_MAUI_Sample
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity, EMDKManager.IEMDKListener/*, EMDKManager.IStatusListener*/
    {
        private readonly string? TAG= "MainActivity";

        private readonly string URI_SERIAL = "content://oem_info/oem.zebra.secure/build_serial";
        private readonly string URI_IMEI = "content://oem_info/wan/imei";
        private readonly string URI_BT_MAC = "content://oem_info/oem.zebra.secure/bt_mac";
        private readonly string URI_WIFI_MAC = "content://oem_info/oem.zebra.secure/wifi_mac";

        private EMDKManager emdkManager;
        private ProfileManager profileManager = null;
        StringBuilder sb;

        protected override void OnPostCreate(Bundle savedInstanceState)
        {

            base.OnPostCreate(savedInstanceState);

            EMDKResults results = EMDKManager.GetEMDKManager(this, this);

            
        }

        void EMDKManager.IEMDKListener.OnClosed()
        {
            if (emdkManager != null)
            {
                emdkManager.Release();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Clean up the objects created by EMDK manager
            if (profileManager != null)
            {
                profileManager = null;
            }

            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }

        void EMDKManager.IEMDKListener.OnOpened(EMDKManager emdkManagerInstance)
        {

            this.emdkManager = emdkManagerInstance;

            try
            {
                profileManager = (ProfileManager) emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Profile);

                string[] modifyData = new string[1];

                String signature = GetCallerSignatureBase64Encoded("com.zebra.oeminfo_maui_sample");  //Android.App.Application.Context.PackageName

                //String service1 = "content://oem_info/oem.zebra.secure/wifi_mac";
                String service1 = "content://oem_info/oem.zebra.secure/build_serial";

                String xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "  <characteristic type=\"Profile\">\n" +
                        "  <parm name=\"ProfileName\" value=\"GRANT_OEM_ACCESS\"/>" +

                        "   <characteristic type=\"AccessMgr\" version=\"8.3\">\n" +
                        "      <parm name=\"emdk_name\" value=\"\"/>\n" +
                        "      <parm name=\"OperationMode\" value=\"1\"/>\n" +
                        "      <parm name=\"ServiceAccessAction\" value=\"4\"/>\n" +
                        "      <parm name=\"ServiceIdentifier\" value=\"" + service1 + "\"/>\n" +
                        "      <parm name=\"CallerPackageName\" value=\"" + "com.zebra.oeminfo_maui_sample" + "\"/>\n" +
                        "      <parm name=\"CallerSignature\" value=\"" + signature + "\"/>\n" +
                        "    </characteristic>\n" +

                        "  </characteristic>";


                modifyData[0] = xml;


                EMDKResults resultsReset = profileManager.ProcessProfile("GRANT_OEM_ACCESS", ProfileManager.PROFILE_FLAG.Reset, modifyData);
                EMDKResults results = profileManager.ProcessProfile("GRANT_OEM_ACCESS", ProfileManager.PROFILE_FLAG.Set, modifyData);
               // sb.AppendLine("ProcessProfileAsync:" + results.StatusCode);
               
                Console.WriteLine("ProcessProfile:" + results.StatusCode);
                Console.WriteLine("ProcessProfile:" + results.StatusString);
                Console.WriteLine(CheckXmlError(results));

                RetrieveOEMInfo(Android.Net.Uri.Parse(URI_SERIAL));
            }
            catch (Exception e)
            {
                //RunOnUiThread(() => statusTextView.Text = e.Message);
                Console.WriteLine("Exception: " + e.StackTrace);
            }
        }


/*
        void EMDKManager.IStatusListener.OnStatus(EMDKManager.StatusData statusData, EMDKBase emdkBase)
        {
            if (statusData.Result == EMDKResults.STATUS_CODE.Success)
            {
                if (statusData.FeatureType == EMDKManager.FEATURE_TYPE.Profile)
                {
                    profileManager = (ProfileManager)emdkBase;
                    profileManager.Data += ProfileManager_Data;


                }



            }
        }
*/
        long begin_time = 0;
        void ProfileManager_Data(object sender, ProfileManager.DataEventArgs e)
        {
            EMDKResults results = e.P0.Result;
            //sb.AppendLine("onData:" + CheckXmlError(results));

            long end_time = DateTime.Now.Ticks;
           // sb.AppendLine("EXEC TIME=" + (end_time - begin_time) / 10000 + "msec");
           // sb.AppendLine("BOOT=" + (SystemClock.ElapsedRealtime()) / 1000 + "sec ago");

           // WeakReferenceMessenger.Default.Send(sb.ToString());
        }


        private string CheckXmlError(EMDKResults results)
        {
            StringReader stringReader = null;
            string checkXmlStatus = "";
            bool isFailure = false;

            try
            {
                if (results.StatusCode == EMDKResults.STATUS_CODE.CheckXml)
                {
                    stringReader = new StringReader(results.StatusString);

                    using (XmlReader reader = XmlReader.Create(stringReader))
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "parm-error":
                                        isFailure = true;
                                        string parmName = reader.GetAttribute("name");
                                        string parmErrorDescription = reader.GetAttribute("desc");
                                        checkXmlStatus = "Name: " + parmName + ", Error Description: " + parmErrorDescription;
                                        break;
                                    case "characteristic-error":
                                        isFailure = true;
                                        string errorType = reader.GetAttribute("type");
                                        string charErrorDescription = reader.GetAttribute("desc");
                                        checkXmlStatus = "Type: " + errorType + ", Error Description: " + charErrorDescription;
                                        break;
                                }
                            }
                        }

                        if (!isFailure)
                        {
                            checkXmlStatus = "Profile applied successfully ...";
                        }

                    }
                }
                else
                {
                    checkXmlStatus = results.StatusCode.ToString();
                }
            }
            finally
            {
                if (stringReader != null)
                {
                    stringReader.Dispose();
                }
            }

            return checkXmlStatus;
        }


        string GetCallerSignatureBase64Encoded(string packageName)
        {
            string callerSignature = null;

            try
            {
                // Get the package manager
                var packageManager = Android.App.Application.Context.PackageManager;

                if (packageManager != null) { 

                    // Get package info with signatures
                    var packageInfo = packageManager.GetPackageInfo(packageName, Android.Content.PM.PackageInfoFlags.Signatures);

     
                    var sig = packageInfo.Signatures[0];
                    
                    if (sig != null)
                        {
                            // Convert to base64
                            byte[] data = Android.Util.Base64.Encode(sig.ToByteArray(), Android.Util.Base64Flags.Default);
                            string signature = System.Text.Encoding.UTF8.GetString(data);
                        callerSignature = signature.Replace("\n", "").Replace("\r", ""); //EMDK ACCESS MANAGER EXPECTS BASE64 ENCODED STRING WITHOUT NEWLINE!

                        Console.WriteLine($"SignatureVerifier: caller signature:{callerSignature}");
                        }
                    }
            }
            catch (Exception ex)
            {

            }

            return callerSignature;
        }


        private void RetrieveOEMInfo(Android.Net.Uri? uri)
        {
            ICursor? cursor = ContentResolver?.Query(uri!, null, null, null, null);
            if (cursor == null || cursor.Count < 1)
            {
                string errorMsg = "Null cursor";
                WeakReferenceMessenger.Default.Send("#1-"+errorMsg);

                return;
            }
            while (cursor.MoveToNext())
            {
                if (cursor.ColumnCount == 0)
                {
                    string errorMsg = "Error: " + uri + " no data";

                    WeakReferenceMessenger.Default.Send("#2-" + errorMsg);
                }
                else
                {
                    for (int i = 0; i < cursor.ColumnCount; i++)
                    {
                        Log.Verbose(TAG, "column " + i + "=" + cursor.GetColumnName(i));
                        try
                        {
                            string? data = cursor.GetString(cursor.GetColumnIndex(cursor.GetColumnName(i)));

                            WeakReferenceMessenger.Default.Send("Serial: " + data);
                        }
                        catch (Exception e)
                        {
                            Log.Info(TAG, "Exception column " + cursor.GetColumnName(i));
                        }
                    }
                }
            }
            cursor.Close();
        }

    }
}
