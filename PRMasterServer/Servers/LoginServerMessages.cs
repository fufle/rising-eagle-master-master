﻿using PRMasterServer.Data;
using Reality.Net.Extensions;
using Reality.Net.GameSpy.Servers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace PRMasterServer.Servers
{
    internal class LoginServerMessages
    {
        private readonly static Random _random = new Random();

        public static byte[] GenerateServerChallenge(ref LoginSocketState state)
        {
            state.ServerChallenge = _random.GetString(10);
            string message = String.Format(@"\lc\1\challenge\{0}\id\1\final\", state.ServerChallenge);
            return DataFunctions.StringToBytes(message);
        }

        public static byte[] SendProof(ref LoginSocketState state, Dictionary<string, string> keyValues)
        {
            string response = String.Empty;
            int requiredValues = 0;
            state.Name = String.Empty;
            string errorMessage = string.Empty; // Variable to hold potential error message

            if (keyValues.ContainsKey("uniquenick")) { state.Name = keyValues["uniquenick"]; requiredValues++; }
            if (keyValues.ContainsKey("challenge")) { state.ClientChallenge = keyValues["challenge"]; requiredValues++; }
            if (keyValues.ContainsKey("response")) { response = keyValues["response"]; requiredValues++; }

            if (requiredValues != 3)
            {
                errorMessage = @"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\";
                Console.WriteLine($"[ERROR] Invalid Query: Required values not met ({requiredValues}/3)");
            }
            else
            {
                Console.WriteLine($"[DEBUG] Received Login Query from client:");
                Console.WriteLine($"[DEBUG]   uniquenick: {state.Name}");
                Console.WriteLine($"[DEBUG]   client challenge: {state.ClientChallenge}");
                Console.WriteLine($"[DEBUG]   client response: {response}");

                var clientData = LoginDatabase.Instance.GetData(state.Name);
                if (clientData != null)
                {
                    state.PasswordEncrypted = (string)clientData["passwordenc"];

                    Console.WriteLine($"[DEBUG] User found in DB: {state.Name}");
                    Console.WriteLine($"[DEBUG]   PasswordEncrypted from DB: {state.PasswordEncrypted}");
                    string decryptedPass = DecryptPassword(state.PasswordEncrypted);
                    Console.WriteLine($"[DEBUG]   Decrypted Password (after gsBase64Decode & gsEncode): {decryptedPass}");

                    string expectedResponse = GenerateResponseValue(ref state);
                    Console.WriteLine($"[DEBUG]   Server calculated EXPECTED response: {expectedResponse}");

                    if (response == expectedResponse)
                    {
                        // ... בתוך ה-if (response == expectedResponse) בלוק ...
                        ushort session = GenerateSession(state.Name);
                        string proofMessage = String.Format(@"\lc\2\sesskey\{0}\proof\{1}\userid\{2}\profileid\{3}\uniquenick\{4}\lt\{5}\id\1\final\",
                                                session, GenerateProofValue(state), clientData["userid"], clientData["profileid"], state.Name,
                                                _random.GetString(22, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ") + "__");

                        Console.WriteLine($"[DEBUG] Login successful for {state.Name}. Sending proof message:");
                        Console.WriteLine($"[DEBUG]   Proof String: {proofMessage}"); // הוסף את השורה הזו!
                                                                                      // ... (שאר הקוד) ...
                        return DataFunctions.StringToBytes(proofMessage);
                        //ushort session = GenerateSession(state.Name);
                        //string proof = String.Format(@"\lc\2\sesskey\{0}\proof\{1}\userid\{2}\profileid\{3}\uniquenick\{4}\lt\{5}\id\1\final\",
                        //                                session, GenerateProofValue(state), clientData["userid"], clientData["profileid"], state.Name,
                        //                                _random.GetString(22, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ][") + "__");
                        //LoginDatabase.Instance.LogLogin(state.Name, ((IPEndPoint)state.Socket.RemoteEndPoint).Address);
                        //state.State++;
                        //Console.WriteLine($"[DEBUG] Login successful for {state.Name}. Sending proof.");
                        //return DataFunctions.StringToBytes(proof); // This is now the ONLY explicit return path for success
                    }
                    else
                    {
                        errorMessage = @"\error\\err\260\fatal\\errmsg\The password provided is incorrect.\id\1\final\";
                        Console.WriteLine($"[ERROR] Login failed for {state.Name}: Client response '{response}' DOES NOT MATCH server expected response '{expectedResponse}'");
                    }
                }
                else // clientData == null
                {
                    errorMessage = String.Format(@"\error\\err\265\fatal\\errmsg\Username [{0}] doesn't exist!\id\1\final\", state.Name);
                    Console.WriteLine($"[ERROR] Login failed: Username [{state.Name}] doesn't exist in database.");
                }
            }

            // If we reached here, it means there was an error (either requiredValues != 3,
            // or clientData was null, or password didn't match).
            // Now, we guarantee a return for all error paths.
            // If errorMessage is still empty here, it means an unhandled scenario occurred.
            if (string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine("[FATAL] SendProof function encountered an unhandled scenario.");
                errorMessage = @"\error\\err\1\fatal\\errmsg\Internal Server Error (Unhandled)!\id\1\final\";
            }

            return DataFunctions.StringToBytes(errorMessage);
        }
        //public static byte[] SendProof(ref LoginSocketState state, Dictionary<string, string> keyValues)
        //{
        //    string response = String.Empty;
        //    int requiredValues = 0;
        //    state.Name = String.Empty;
        //    if (keyValues.ContainsKey("uniquenick")) { state.Name = keyValues["uniquenick"]; requiredValues++; }
        //    if (keyValues.ContainsKey("challenge")) { state.ClientChallenge = keyValues["challenge"]; requiredValues++; }
        //    if (keyValues.ContainsKey("response")) { response = keyValues["response"]; requiredValues++; }
        //    if (requiredValues != 3)
        //        return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\");
        //    var clientData = LoginDatabase.Instance.GetData(state.Name);
        //    if (clientData != null)
        //    {
        //        state.PasswordEncrypted = (string)clientData["passwordenc"];
        //        if (response == GenerateResponseValue(ref state))
        //        {
        //            ushort session = GenerateSession(state.Name);
        //            string proof = String.Format(@"\lc\2\sesskey\{0}\proof\{1}\userid\{2}\profileid\{3}\uniquenick\{4}\lt\{5}\id\1\final\",
        //                session, GenerateProofValue(state), clientData["userid"], clientData["profileid"], state.Name,
        //                _random.GetString(22, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ][") + "__");
        //            LoginDatabase.Instance.LogLogin(state.Name, ((IPEndPoint)state.Socket.RemoteEndPoint).Address);
        //            state.State++;
        //            return DataFunctions.StringToBytes(proof);
        //        }
        //        else
        //        {
        //            return DataFunctions.StringToBytes(@"\error\\err\260\fatal\\errmsg\The password provided is incorrect.\id\1\final\");
        //        }
        //    }
        //    else
        //    {
        //        return DataFunctions.StringToBytes(String.Format(@"\error\\err\265\fatal\\errmsg\Username [{0}] doesn't exist!\id\1\final\", state.Name));
        //    }
        //}

        public static byte[] SendProfile(ref LoginSocketState state, Dictionary<string, string> keyValues, bool retrieve)
        {
            var clientData = LoginDatabase.Instance.GetData(state.Name);
            if (clientData == null)
            {
                return DataFunctions.StringToBytes(String.Format(@"\error\\err\265\fatal\\errmsg\Username [{0}] doesn't exist!\id\1\final\", state.Name));
            }
            string message = String.Format(
                @"\pi\\profileid\{0}\nick\{1}\userid\{2}\email\{3}\sig\{4}\uniquenick\{5}\pid\{6}" +
                @"\countrycode\{7}\lon\{8}\lat\{9}\id\{10}\final\",
                clientData["profileid"],                        // {0}
                state.Name,                                     // {1}
                clientData["userid"],                           // {2}
                clientData["email"],                            // {3}
                _random.GetString(32, "0123456789abcdef"),   // {4}
                state.Name,                                     // {5}
                clientData["profileid"],                        // {6}
                clientData["country"] ?? "??",                  // {7}
                "0.000000",                                     // {8}
                "0.000000",                                     // {9}
                keyValues["id"]                                 // {10}
            );
            if (!retrieve)
                state.State++;
            return DataFunctions.StringToBytes(message);
        }

        public static void UpdateProfile(ref LoginSocketState state, Dictionary<string, string> keyValues)
        {
            string country = "??";
            if (keyValues.ContainsKey("countrycode"))
                country = keyValues["countrycode"].ToUpperInvariant();
            Dictionary<string, object> clientData = new Dictionary<string, object>() { { "country", country } };
            LoginDatabase.Instance.SetData(state.Name, clientData);
            state.State++;
        }

        public static void Logout(ref LoginSocketState state, Dictionary<string, string> keyValues)
        {
            state.Dispose();
        }

        public static byte[] NewUser(ref LoginSocketState state, Dictionary<string, string> keyValues)
        {
            string message;
            if (keyValues.ContainsKey("nick")) { state.Name = keyValues["nick"]; } else { return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\"); }
            if (keyValues.ContainsKey("email")) { state.Email = keyValues["email"]; } else { return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\"); }
            if (keyValues.ContainsKey("passenc")) { state.PasswordEncrypted = keyValues["passenc"]; } else { return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\"); }
            if (LoginDatabase.Instance.UserExists(state.Name))
            {
                return DataFunctions.StringToBytes(@"\error\\err\516\fatal\\errmsg\This account name is already in use!\id\1\final\");
            }
            else
            {
                string password = DecryptPassword(state.PasswordEncrypted);
                LoginDatabase.Instance.CreateUser(state.Name, password.ToMD5(), state.Email, "??", ((IPEndPoint)state.Socket.RemoteEndPoint).Address);
                var clientData = LoginDatabase.Instance.GetData(state.Name);
                if (clientData == null) { return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Error creating account!\id\1\final\"); }
                message = String.Format(@"\nur\0\pid\{0}\final\", clientData["profileid"]);
            }
            return DataFunctions.StringToBytes(message);
        }

        public static byte[] SendKeepAlive()
        {
            return DataFunctions.StringToBytes(@"\ka\\final\");
        }

        public static byte[] SendHeartbeat()
        {
            return DataFunctions.StringToBytes(String.Format(@"\lt\{0}\final\", _random.GetString(22, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ][") + "__"));
        }

        internal static string PasswordFromKeyValues(Dictionary<string, string> keyValues)
        {
            if (keyValues.ContainsKey("passenc")) { return DecryptPassword(keyValues["passenc"]); }
            else if (keyValues.ContainsKey("pass")) { return keyValues["pass"]; }
            return String.Empty;
        }

        internal static byte[] SendNicks(ref LoginSocketState state, Dictionary<string, string> keyValues)
        {
            if (!keyValues.ContainsKey("email") || (!keyValues.ContainsKey("passenc") && !keyValues.ContainsKey("pass"))) { return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\"); }
            string password = PasswordFromKeyValues(keyValues);
            password = password.ToMD5();
            var clientData = LoginDatabase.Instance.GetData(keyValues["email"], password);
            if (clientData == null) { return DataFunctions.StringToBytes(@"\error\\err\551\fatal\\errmsg\Unable to get any associated profiles.\id\1\final\"); }
            List<string> nicks = new List<string>();
            foreach (var client in clientData) { nicks.Add((string)client["name"]); }
            if (nicks.Count == 0) { return DataFunctions.StringToBytes(@"\nr\0\ndone\\final\"); }
            state.State++;
            return DataFunctions.StringToBytes(GenerateNicks(nicks.ToArray()));
        }

        private static string GenerateNicks(string[] nicks)
        {
            string message = @"\nr\" + nicks.Length;
            for (int i = 0; i < nicks.Length; i++) { message += String.Format(@"\nick\{0}\uniquenick\{0}", nicks[i]); }
            message += @"\ndone\final\";
            return message;
        }

        internal static byte[] SendCheck(ref LoginSocketState state, Dictionary<string, string> keyValues)
        {
            string name = String.Empty;
            if (String.IsNullOrWhiteSpace(name)) { if (keyValues.ContainsKey("uniquenick")) { name = keyValues["uniquenick"]; } }
            if (String.IsNullOrWhiteSpace(name)) { if (keyValues.ContainsKey("nick")) { name = keyValues["nick"]; } }
            if (String.IsNullOrWhiteSpace(name)) { return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\"); }
            var clientData = LoginDatabase.Instance.GetData(name);
            if (clientData == null) { return DataFunctions.StringToBytes(String.Format(@"\error\\err\265\fatal\\errmsg\Username [{0}] doesn't exist!\id\1\final\", name)); }
            string message;
            string password = PasswordFromKeyValues(keyValues);
            if (LoginDatabase.Instance.CheckPasswordIsCorrect((Int64)clientData["id"], password)) { message = String.Format(@"\cur\0\pid\{0}\final\", clientData["profileid"]); }
            else { message = String.Format(@"\cur\3587\pid\{0}\final\", clientData["profileid"]); }
            return DataFunctions.StringToBytes(message);
        }

        internal static byte[] SendSearch(ref LoginSocketState state, Dictionary<string, string> keyValues)
        {
            string message = "";
            if (keyValues.ContainsKey("uniquenick")) { message = GenerateSearchResultsNick(LoginDatabase.Instance.GetData(keyValues["uniquenick"])); }
            else if (keyValues.ContainsKey("email"))
            {
                List<Dictionary<string, object>> matchedUsers;
                matchedUsers = LoginDatabase.Instance.GetUserByEmail(keyValues["email"]);
                message = GenerateSearchResultsEmail(matchedUsers);
            }
            else { return DataFunctions.StringToBytes(@"\error\\err\0\fatal\\errmsg\Invalid Query!\id\1\final\"); }
            return DataFunctions.StringToBytes(message);
        }

        private static string GenerateSearchResultsNick(Dictionary<string, object> user)
        {
            string message = "";
            if (user != null) { message += String.Format(@"\bsr\{0}\nick\{1}\uniquenick\{1}\namespaceid\2", user["profileid"], user["name"]); }
            message += @"\bsrdone\final\more\0\";
            return message;
        }

        private static string GenerateSearchResultsEmail(List<Dictionary<string, object>> users)
        {
            string message = "";
            if (users != null) { foreach (Dictionary<string, object> user in users) { message += String.Format(@"\bsr\{0}\nick\{1}\uniquenick\{1}\email\{2}\namespaceid\2", user["profileid"], user["name"], user["email"]); } }
            message += @"\bsrdone\final\more\0\";
            return message;
        }

        private static string GenerateProofValue(LoginSocketState state)
        {
            string value = state.PasswordEncrypted;
            value += new String(' ', 48);
            value += state.Name;
            value += state.ClientChallenge;
            value += state.ServerChallenge;
            value += state.PasswordEncrypted;
            return value.ToMD5();
        }

        private static string GenerateResponseValue(ref LoginSocketState state)
        {
            string value = state.PasswordEncrypted;
            value += new String(' ', 48);
            value += state.Name;
            value += state.ClientChallenge;
            value += state.ServerChallenge;
            value += state.PasswordEncrypted;
            return value.ToMD5();
        }

        private static ushort GenerateSession(string name)
        {
            ushort[] crc_table = new ushort[256] {
                0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
                0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
                0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
                0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
                0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
                0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
                0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
                0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
                0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
                0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
                0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
                0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
                0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
                0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
                0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
                0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040
            };
            int len = name.Length;
            int nameIndex = 0;
            ushort session = 0;
            while (len-- != 0)
            {
                session = (ushort)(crc_table[((name[nameIndex] ^ session) & 0xff) % 256] ^ (session >> 8));
                nameIndex++;
            }
            return session;
        }

        private static string DecryptPassword(string password)
        {
            string decrypted = gsBase64Decode(password, password.Length);
            gsEncode(ref decrypted);
            return decrypted;
        }

        public static int gsEncode(ref string password)
        {
            byte[] pass = DataFunctions.StringToBytes(password);
            int i, a, c, d, num = 0x79707367;
            int passlen = pass.Length;
            if (num == 0) num = 1;
            else num &= 0x7fffffff;
            for (i = 0; i < passlen; i++)
            {
                d = 0xff; c = 0; d -= c;
                if (d != 0) { num = gsLame(num); a = num % d; a += c; }
                else a = c;
                pass[i] ^= (byte)(a % 256);
            }
            password = DataFunctions.BytesToString(pass);
            return passlen;
        }

        private static int gsLame(int num)
        {
            int a, c = (num >> 16) & 0xffff;
            a = num & 0xffff; c *= 0x41a7; a *= 0x41a7; a += ((c & 0x7fff) << 16);
            if (a < 0) { a &= 0x7fffffff; a++; }
            a += (c >> 15);
            if (a < 0) { a &= 0x7fffffff; a++; }
            return a;
        }

        private static string gsBase64Decode(string s, int size)
        {
            byte[] data = DataFunctions.StringToBytes(s);
            int len, xlen, a = 0, b = 0, c = 0, step, limit, y = 0, z = 0;
            byte[] buff, p;
            char[] basechars = new char[128] {
                '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00',
                '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00',
                '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00', '\x3e', '\x00', '\x00', '\x00', '\x3f',
                '\x34', '\x35', '\x36', '\x37', '\x38', '\x39', '\x3a', '\x3b', '\x3c', '\x3d', '\x00', '\x00', '\x00', '\x00', '\x00', '\x00',
                '\x00', '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07', '\x08', '\x09', '\x0a', '\x0b', '\x0c', '\x0d', '\x0e',
                '\x0f', '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19', '\x3e', '\x00', '\x3f', '\x00', '\x00',
                '\x00', '\x1a', '\x1b', '\x1c', '\x1d', '\x1e', '\x1f', '\x20', '\x21', '\x22', '\x23', '\x24', '\x25', '\x26', '\x27', '\x28',
                '\x29', '\x2a', '\x2b', '\x2c', '\x2d', '\x2e', '\x2f', '\x30', '\x31', '\x32', '\x33', '\x00', '\x00', '\x00', '\x00', '\x00'
            };
            if (size <= 0) len = data.Length; else len = size;
            xlen = ((len >> 2) * 3) + 1;
            buff = new byte[xlen % 256];
            if (buff.Length == 0) return null;
            p = buff;
            limit = data.Length + len;
            for (step = 0; ; step++)
            {
                do
                {
                    if (z >= limit) { c = 0; break; }
                    if (z < data.Length) c = data[z]; else c = 0;
                    z++;
                    if ((c == '=') || (c == '_')) { c = 0; break; }
                } while (c != 0 && ((c <= (byte)' ') || (c > 0x7f)));
                if (c == 0) break;
                switch (step & 3)
                {
                    case 0: a = basechars[c]; break;
                    case 1: b = basechars[c]; p[y++] = (byte)(((a << 2) | (b >> 4)) % 256); break;
                    case 2: a = basechars[c]; p[y++] = (byte)((((b & 15) << 4) | (a >> 2)) % 256); break;
                    case 3: p[y++] = (byte)((((a & 3) << 6) | basechars[c]) % 256); break;
                    default: break;
                }
            }
            p[y] = 0;
            len = p.Length - buff.Length;
            if (size != 0) size = len;
            if ((len + 1) != xlen) if (buff.Length == 0) return null;
            return DataFunctions.BytesToString(buff).Substring(0, y);
        }
    }
}