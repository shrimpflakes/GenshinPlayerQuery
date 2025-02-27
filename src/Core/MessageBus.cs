﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using GenshinPlayerQuery.Model;
using GenshinPlayerQuery.View;
using Newtonsoft.Json.Linq;

namespace GenshinPlayerQuery.Core
{
    internal static class MessageBus
    {
        private const int COOKIE_HTTP_ONLY = 0x00002000;
        private const string COOKIE_URL = "https://user.mihoyo.com/";

        private const string LOGIN_TICKET_FILE = "ticket.txt";
        private const string QUERY_HISTORY_FILE = "history.txt";
        private const int MAX_QUERY_HISTORY_COUNT = 10;

        public static PlayerQueryResult PlayerQueryResult { get; set; }

        static MessageBus()
        {
            new Thread(UpdateChecker.CheckUpdate).Start();

            if (File.Exists(LOGIN_TICKET_FILE))
            {
                LoginTicket = File.ReadAllText(LOGIN_TICKET_FILE);
            }

            if (File.Exists(QUERY_HISTORY_FILE))
            {
                try
                {
                    string queryHistory = File.ReadAllText(QUERY_HISTORY_FILE);
                    QueryHistory.AddRange(Regex.Split(queryHistory.Trim(), "\r\n|\r|\n"));
                }
                catch
                {
                    MessageBox.Show("查询历史记录解析失败");
                }
            }
        }

        public static string LoginTicket { get; set; }
        public static List<string> QueryHistory { get; set; } = new List<string>();
        public static LoginWindow LoginWindow { get; set; }
        public static MainWindow MainWindow { get; set; }

        public static void AddQueryHistory(string uid)
        {
            if (!QueryHistory.Contains(uid))
            {
                MainWindow.ComboBoxUserId.Items.Add(uid);
                QueryHistory.Add(uid);
                if (QueryHistory.Count > MAX_QUERY_HISTORY_COUNT)
                {
                    MainWindow.ComboBoxUserId.Items.RemoveAt(0);
                    QueryHistory.RemoveAt(0);
                }
            }
        }

        public static void Exit()
        {
            StringBuilder queryHistory = new StringBuilder();
            foreach (string uid in QueryHistory)
            {
                queryHistory.AppendLine(uid);
            }

            File.WriteAllText(QUERY_HISTORY_FILE, queryHistory.ToString());
            Environment.Exit(0);
        }

        public static void Login()
        {
            MainWindow.Visibility = Visibility.Hidden;
            new LoginWindow().Show();
        }

        public static void AfterLoginSuccessful()
        {
            LoginTicket = GetBrowserLoginTicket();
            File.WriteAllText(LOGIN_TICKET_FILE, LoginTicket);
            LoginWindow.Close();
            MainWindow.Visibility = Visibility.Visible;
        }

        public static void AfterLoginFailed()
        {
            MessageBox.Show("工具需要您的米游社Cookie来调用查询接口\r\n此操作不会泄露您的账号信息", "提示", MessageBoxButton.OK);
            Exit();
        }

        public static void ShowRoleDetails(string roleId)
        {
            string role = JObject.Parse(PlayerQueryResult.Roles)["avatars"].First(x => x["id"].ToString() == roleId).ToString();
            new RoleWindow(PageRender.RenderRolePage(role)).Show();
        }

        public static string GetBrowserLoginTicket()
        {
            StringBuilder loginTicket = new StringBuilder(1024);
            uint size = Convert.ToUInt32(loginTicket.Capacity + 1);
            InternetGetCookieEx(COOKIE_URL, null, loginTicket, ref size, COOKIE_HTTP_ONLY, IntPtr.Zero);
            return loginTicket.ToString();
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetGetCookieEx(
            string url, string cookieName, StringBuilder cookieData,
            ref uint cookieSize, int flags, IntPtr reversed);
    }
}