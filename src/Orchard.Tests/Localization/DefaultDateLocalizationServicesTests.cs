﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Orchard.Localization.Models;
using Orchard.Localization.Services;
using Orchard.Services;

namespace Orchard.Tests.Localization {

    [TestFixture]
    public class DefaultDateLocalizationServicesTests {

        [SetUp]
        public void Init() {
            //Regex.CacheSize = 1024;
        }

        [Test]
        [Description("Date component is decremented by one day when converting to time zone with negative offset greater than time component.")]
        public void ConvertToSiteTimeZoneTest01() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Utc);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.LessThan(TimeSpan.FromHours(-3)));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(14, result.Day);
        }

        [Test]
        [Description("Date component is incremented by one day when converting to time zone with positive offset greater than 24 hours minus time component.")]
        public void ConvertToSiteTimeZoneTest02() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Utc);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.GreaterThan(TimeSpan.FromHours(3)));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(16, result.Day);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Utc is converted to DateTimeKind.Local with offset when target time zone is not UTC.")]
        public void ConvertToSiteTimeZoneTest03() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Utc);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.Not.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(DateTimeKind.Local, result.Kind);
            Assert.AreEqual(dateTimeUtc.Hour + timeZone.BaseUtcOffset.Hours, result.Hour);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Utc is not converted when target time zone is UTC.")]
        public void ConvertToSiteTimeZoneTest04() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Utc);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(dateTimeUtc, result);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Unspecified is converted to DateTimeKind.Local with offset when target time zone is not UTC.")]
        public void ConvertToSiteTimeZoneTest05() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Unspecified);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.Not.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(DateTimeKind.Local, result.Kind);
            Assert.AreEqual(dateTimeUtc.Hour + timeZone.BaseUtcOffset.Hours, result.Hour);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Unspecified is converted to DateTimeKind.Utc with no offset when target time zone is UTC.")]
        public void ConvertToSiteTimeZoneTest06() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Unspecified);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(dateTimeUtc, result);
        }

        [Test]
        [Description("DateTime which is already DateTimeKind.Local is never converted.")]
        public void ConvertToSiteTimeZoneTest07() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Local);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.Not.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(DateTimeKind.Local, result.Kind);
            Assert.AreEqual(dateTimeUtc, result);
        }

        [Test]
        [Description("Resulting DateTime is DateTimeKind.Local even when target time zone is not configured time zone of local computer.")]
        public void ConvertToSiteTimeZoneTest08() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            if (timeZone == TimeZoneInfo.Local) {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeUtc = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Utc);
            Assert.That(timeZone.GetUtcOffset(dateTimeUtc), Is.Not.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteTimeZone(dateTimeUtc);
            Assert.AreEqual(DateTimeKind.Local, result.Kind);
        }

        [Test]
        [Description("Date component is incremented by one day when converting from time zone with negative offset greater than 24 hours minus time component.")]
        public void ConvertFromSiteTimeZoneTest01() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeLocal = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Local);
            Assert.That(timeZone.GetUtcOffset(dateTimeLocal), Is.LessThan(TimeSpan.FromHours(-3)));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteTimeZone(dateTimeLocal);
            Assert.AreEqual(16, result.Day);
        }

        [Test]
        [Description("Date component is decremented by one day when converting from time zone with positive offset greater than time component.")]
        public void ConvertFromSiteTimeZoneTest02() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeLocal = new DateTime(1998, 1, 15, 3, 0, 0, DateTimeKind.Local);
            Assert.That(timeZone.GetUtcOffset(dateTimeLocal), Is.GreaterThan(TimeSpan.FromHours(3)));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteTimeZone(dateTimeLocal);
            Assert.AreEqual(14, result.Day);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Local is converted to DateTimeKind.Utc with offset when target time zone is not UTC.")]
        public void ConvertFromSiteTimeZoneTest03() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeLocal = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Local);
            Assert.That(timeZone.GetUtcOffset(dateTimeLocal), Is.Not.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteTimeZone(dateTimeLocal);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(dateTimeLocal.Hour - timeZone.BaseUtcOffset.Hours, result.Hour);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Local is converted to DateTimeKind.Utc with no offset when target time zone is UTC.")]
        public void ConvertFromSiteTimeZoneTest04() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeLocal = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Local);
            Assert.That(timeZone.GetUtcOffset(dateTimeLocal), Is.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteTimeZone(dateTimeLocal);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(dateTimeLocal.Hour, result.Hour);
            Assert.AreEqual(dateTimeLocal.Minute, result.Minute);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Unspecified is converted to DateTimeKind.Utc with offset when target time zone is not UTC.")]
        public void ConvertFromSiteTimeZoneTest05() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeLocal = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Unspecified);
            Assert.That(timeZone.GetUtcOffset(dateTimeLocal), Is.Not.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteTimeZone(dateTimeLocal);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(dateTimeLocal.Hour - timeZone.BaseUtcOffset.Hours, result.Hour);
        }

        [Test]
        [Description("DateTime which is DateTimeKind.Unspecified is converted to DateTimeKind.Utc with no offset when target time zone is UTC.")]
        public void ConvertFromSiteTimeZoneTest06() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeLocal = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Unspecified);
            Assert.That(timeZone.GetUtcOffset(dateTimeLocal), Is.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteTimeZone(dateTimeLocal);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(dateTimeLocal.Hour, result.Hour);
            Assert.AreEqual(dateTimeLocal.Minute, result.Minute);
        }

        [Test]
        [Description("DateTime which is already DateTimeKind.Utc is never converted.")]
        public void ConvertFromSiteTimeZoneTest07() {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var container = TestHelpers.InitializeContainer(null, null, timeZone);
            var dateTimeLocal = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Utc);
            Assert.That(timeZone.GetUtcOffset(dateTimeLocal), Is.Not.EqualTo(TimeSpan.Zero));
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteTimeZone(dateTimeLocal);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(dateTimeLocal, result);
        }

        [Test]
        [Description("Converting to Gregorian calendar yields a DateTimeParts instance equivalent to the original DateTime.")]
        public void ConvertToSiteCalendarTest01() {
            var container = TestHelpers.InitializeContainer(null, "GregorianCalendar", TimeZoneInfo.Utc);
            var dateTimeGregorian = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Utc);
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteCalendar(dateTimeGregorian, TimeSpan.Zero);
            var expected = new DateTimeParts(1998, 1, 15, 21, 0, 0, 0, DateTimeKind.Utc, TimeSpan.Zero);
            Assert.AreEqual(expected, result);
        }

        [Test]
        [Description("Converting to non-Gregorian calendar yields a DateTimeParts instance with correct values.")]
        public void ConvertToSiteCalendarTest02() {
            var container = TestHelpers.InitializeContainer(null, "PersianCalendar", TimeZoneInfo.Utc);
            var dateTimeGregorian = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Utc);
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertToSiteCalendar(dateTimeGregorian, TimeSpan.Zero);
            var expected = new DateTimeParts(1376, 10, 25, 21, 0, 0, 0, DateTimeKind.Utc, TimeSpan.Zero);
            Assert.AreEqual(expected, result);
        }

        [Test]
        [Description("Converting from Gregorian calendar yields a DateTime equivalent to the original DateTimeParts instance.")]
        public void ConvertFromSiteCalendarTest01() {
            var container = TestHelpers.InitializeContainer(null, "GregorianCalendar", TimeZoneInfo.Utc);
            var dateTimePartsGregorian = new DateTimeParts(1998, 1, 15, 21, 0, 0, 0, DateTimeKind.Utc, TimeSpan.Zero);
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteCalendar(dateTimePartsGregorian);
            var expected = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(expected, result);
        }

        [Test]
        [Description("Converting from non-Gregorian calendar yields a DateTime with correct values.")]
        public void ConvertFromSiteCalendarTest02() {
            var container = TestHelpers.InitializeContainer(null, "PersianCalendar", TimeZoneInfo.Utc);
            var dateTimePartsPersian = new DateTimeParts(1376, 10, 25, 21, 0, 0, 0, DateTimeKind.Utc, TimeSpan.Zero);
            var target = container.Resolve<IDateLocalizationServices>();
            var result = target.ConvertFromSiteCalendar(dateTimePartsPersian);
            var expected = new DateTime(1998, 1, 15, 21, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(expected, result);
        }

        /*
            ConvertToLocalizedTimeStringTest
	            Time zone conversion works properly (even though there is no date component).

            ConvertToLocalizedStringTest
	            Non-nullable DateTime.MinValue is converted to NullText.
	            Nullable DateTime.MinValue is converted to date/time string.
	            Nullable null is converted to NullText.
	            Time zone conversion is performed when EnableTimeZoneConversion is true.
	            Time zone conversion is not performed when EnableTimeZoneConversion is false.
	            Calendar conversion is performed when EnableCalendarConversion is true.
	            Calendar conversion is not performed when EnableCalendarConversion is false.
	            Full conversion is performed with default options.
	
            ConvertFromLocalizedStringCombinedTest
	            Null date/time string is converted to null.
	            Empty date/time string is converted to null.
	            Custom NullText date/time string is converted to null.
	            Time zone conversion is performed when EnableTimeZoneConversion is true.
	            Time zone conversion is not performed when EnableTimeZoneConversion is false.
	            Calendar conversion is performed when EnableCalendarConversion is true.
	            Calendar conversion is not performed when EnableCalendarConversion is false.
	            Full conversion is performed with default options.
	
            ConvertFromLocalizedStringSeparateTest	
	            Null date string and time string is converted to null.
	            Empty date string and time string is converted to null.
	            Custom NullText date string and time string is converted to null.
	            Time zone conversion works properly when date component is omitted.
	            Time zone conversion is never performed when time component is omitted.
	            Time zone conversion is performed when EnableTimeZoneConversion is true.
	            Time zone conversion is not performed when EnableTimeZoneConversion is false.
	            Calendar conversion is never performed when date component is omitted.
	            Calendar conversion is performed when EnableCalendarConversion is true.
	            Calendar conversion is not performed when EnableCalendarConversion is false.
	            Full conversion is performed with default options.
	    */
    }
}
