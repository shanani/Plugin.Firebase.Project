﻿using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Firebase.Messaging;
using Plugin.Firebase.CloudMessaging;

namespace Plugin.Firebase.Android.CloudMessaging
{
    public static class FCMNotificationExtensions
    {
        private const string BundleKeyTitle = "title";
        private const string BundleKeyBody = "body";
        private const string BundleKeyData = "data";
        
        public static FCMNotification ToFCMNotification(this RemoteMessage message)
        {
            var notification = message.GetNotification();
            return new FCMNotification(notification?.Body, notification?.Title, message.Data);
        }

        public static FCMNotification ToFCMNotification(this Bundle bundle)
        {
            return new FCMNotification(
                bundle.GetString(BundleKeyBody),
                bundle.GetString(BundleKeyTitle), 
                bundle.GetBundle(BundleKeyData).ToDictionary());
        }

        public static Bundle ToBundle(this FCMNotification notification)
        {
            var bundle = new Bundle();
            bundle.PutString(BundleKeyBody, notification.Body);
            bundle.PutString(BundleKeyTitle, notification.Title);
            bundle.PutBundle(BundleKeyData, notification.Data?.ToBundle());
            return bundle;
        }

        public static FCMNotification GetNotificationFromExtras(this Intent intent, string extraName)
        {
            if(intent.HasExtra(extraName)) {
                return intent.GetBundleExtra(extraName).ToFCMNotification();
            } else {
                return new FCMNotification(
                    intent.Extras.GetString(BundleKeyBody),
                    intent.Extras.GetString(BundleKeyTitle),
                    intent.Extras.ToDictionary());
            }
        }
    }
}