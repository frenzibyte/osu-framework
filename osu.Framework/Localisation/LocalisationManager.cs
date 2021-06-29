﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Configuration;

#nullable enable

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private readonly List<LocaleMapping> locales = new List<LocaleMapping>();

        private readonly Bindable<bool> preferUnicode;
        private readonly Bindable<string> configLocale;
        private readonly Bindable<ILocalisationStore?> currentStorage = new Bindable<ILocalisationStore?>();

        public LocalisationManager(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkSetting.ShowUnicode);

            configLocale = config.GetBindable<string>(FrameworkSetting.Locale);
            configLocale.BindValueChanged(updateLocale);
        }

        public void AddLanguage(string language, ILocalisationStore storage)
        {
            locales.Add(new LocaleMapping(language, storage));
            configLocale.TriggerChange();
        }

        /// <summary>
        /// Creates an <see cref="ILocalisedBindableString"/> which automatically updates its text according to information provided in <see cref="ILocalisedBindableString.Text"/>.
        /// </summary>
        /// <returns>The <see cref="ILocalisedBindableString"/>.</returns>
        public ILocalisedBindableString GetLocalisedString(LocalisableString original) => new LocalisedBindableString(original, currentStorage, preferUnicode);

        private void updateLocale(ValueChangedEvent<string> locale)
        {
            if (locales.Count == 0)
                return;

            var validLocale = locales.Find(l => l.Name == locale.NewValue);

            if (validLocale == null)
            {
                var culture = string.IsNullOrEmpty(locale.NewValue) ? CultureInfo.CurrentCulture : new CultureInfo(locale.NewValue);

                for (var c = culture; !EqualityComparer<CultureInfo>.Default.Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
                {
                    validLocale = locales.Find(l => l.Name == c.Name);
                    if (validLocale != null)
                        break;
                }

                validLocale ??= locales[0];
            }

            currentStorage.Value = validLocale.Storage;
        }

        private class LocaleMapping
        {
            public readonly string Name;
            public readonly ILocalisationStore Storage;

            public LocaleMapping(string name, ILocalisationStore storage)
            {
                Name = name;
                Storage = storage;
            }
        }
    }
}
