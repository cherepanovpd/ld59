using System.Globalization;

namespace Common.Runtime.Utilities
{
    public static class FormatNumber
    {
        // Массив суффиксов (можно расширить при необходимости)
        private static readonly string[] Suffixes =
        {
            "",
            "k",
            "M",
            "B",
            "T",
            "Qa", 
            "Qi",
            "Sx",
            "Sp",
            "Oc",
            "No",
            "Dc"
        };

        // ---------- Форматирование чисел ----------
        /// <summary>
        /// Форматирует число по правилам:
        /// - ≤ 4 символов, с суффиксами (k, M, B...) при необходимости.
        /// - Для чисел < 10000 отображается целая часть (без дробной).
        /// - Для чисел ≥ 10000 применяется суффикс и округление:
        ///   * если после деления число < 10 — формат X.X + суффикс (4 символа)
        ///   * если ≥ 10 — округление до целого + суффикс (≤4 символов)
        /// </summary>
        public static string Format(float value, bool isSuffixWithSpace)
        {
            bool negative = value < 0;
            float absValue = System.Math.Abs(value);

            // Числа меньше 10000 отображаем без суффикса, целыми (до 4 цифр)
            if (absValue < 10000)
            {
                double roundValue = System.Math.Round(absValue, absValue > 10 ? 0 : 1);

                return $"{(negative ? "-" : "")}{roundValue}{(isSuffixWithSpace ? " " : "")}";
            }

            // Определяем подходящий суффикс и масштабируем число
            float scaled = absValue;
            int suffixIndex = 0;

            while (scaled >= 1000 && suffixIndex < Suffixes.Length - 1)
            {
                scaled /= 1000;
                suffixIndex++;
            }

            string formatted = scaled.ToString("0.0", CultureInfo.InvariantCulture);

            return $"{(negative ? "-" : "")}{formatted}{(isSuffixWithSpace ? " " : "")}{Suffixes[suffixIndex]}";
        }

        public static string MyFormat(this float value, bool isSuffixWithSpace)
            => Format(value, isSuffixWithSpace);
    }
}