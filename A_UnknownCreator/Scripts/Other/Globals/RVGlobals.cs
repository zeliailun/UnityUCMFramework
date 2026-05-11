using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public enum RandomChannel
    {
        Global,
        Combat,
        Loot,
        Level,
        Visual,
    }

    public static class RVGlobals
    {
        public static int GaussianMaxAttempts = 100;

        private const string CharContents = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private static int seed;

        private static readonly Dictionary<RandomChannel, Random> randomDict = new();

        static RVGlobals()
        {
            SetSeed(Environment.TickCount);
        }

        public static void SetSeed(int newSeed)
        {
            seed = newSeed;
            randomDict.Clear();

            randomDict[RandomChannel.Global] = new Random(seed);
            randomDict[RandomChannel.Combat] = new Random(seed + 1001);
            randomDict[RandomChannel.Loot] = new Random(seed + 2001);
            randomDict[RandomChannel.Level] = new Random(seed + 3001);
            randomDict[RandomChannel.Visual] = new Random(seed + 4001);
        }

        public static void SetSeed(int newSeed, RandomChannel channel)
        {
            randomDict[channel] = new Random(newSeed);
        }

        private static Random GetRandom(RandomChannel channel)
        {
            if (randomDict.TryGetValue(channel, out Random random))
                return random;

            int channelSeed = seed + ((int)channel + 1) * 1001;
            random = new Random(channelSeed);
            randomDict.Add(channel, random);
            return random;
        }

        public static int RandomInt(
            int min,
            int max,
            bool includeMax = false,
            RandomChannel channel = RandomChannel.Global)
        {
            if (includeMax)
            {
                if (max == int.MaxValue)
                    return GetRandom(channel).Next(min, max);

                max += 1;
            }

            if (max <= min)
                return min;

            return GetRandom(channel).Next(min, max);
        }

        public static float RandomFloat(
            float min,
            float max,
            RandomChannel channel = RandomChannel.Global)
        {
            if (max <= min)
                return min;

            return (float)(GetRandom(channel).NextDouble() * (max - min) + min);
        }

        public static float RandomFloat(RandomChannel channel = RandomChannel.Global)
        {
            return (float)GetRandom(channel).NextDouble();
        }

        public static bool RandomBool(RandomChannel channel = RandomChannel.Global)
        {
            return GetRandom(channel).Next(0, 2) == 0;
        }

        /// <summary>
        /// chance 使用 0~1，比如 0.25f = 25%
        /// </summary>
        public static bool RandomChance(float chance, RandomChannel channel = RandomChannel.Global)
        {
            if (chance <= 0f)
                return false;

            if (chance >= 1f)
                return true;

            return RandomFloat(channel) < chance;
        }

        /// <summary>
        /// percentage 使用 0~100，比如 25 = 25%
        /// </summary>
        public static bool PercChance(float percentage, RandomChannel channel = RandomChannel.Global)
        {
            if (percentage <= 0f)
                return false;

            if (percentage >= 100f)
                return true;

            return RandomFloat(0f, 100f, channel) < percentage;
        }

        public static bool PercChance(double percentage, RandomChannel channel = RandomChannel.Global)
        {
            return PercChance((float)percentage, channel);
        }

        public static T RandomElement<T>(T[] array, RandomChannel channel = RandomChannel.Global)
        {
            if (array == null || array.Length == 0)
                return default;

            return array[RandomInt(0, array.Length, false, channel)];
        }

        public static T RandomElement<T>(IList<T> list, RandomChannel channel = RandomChannel.Global)
        {
            if (list == null || list.Count == 0)
                return default;

            return list[RandomInt(0, list.Count, false, channel)];
        }

        public static KeyValuePair<TKey, TValue> RandomElement<TKey, TValue>(
            Dictionary<TKey, TValue> dict,
            RandomChannel channel = RandomChannel.Global)
        {
            if (dict == null || dict.Count == 0)
                return default;

            int index = RandomInt(0, dict.Count, false, channel);
            int currentIndex = 0;

            foreach (var pair in dict)
            {
                if (currentIndex == index)
                    return pair;

                currentIndex++;
            }

            return default;
        }

        public static void ShuffleArray<T>(T[] array, RandomChannel channel = RandomChannel.Global)
        {
            if (array == null || array.Length <= 1)
                return;

            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomInt(0, i, true, channel);
                (array[j], array[i]) = (array[i], array[j]);
            }
        }

        public static void ShuffleList<T>(IList<T> list, RandomChannel channel = RandomChannel.Global)
        {
            if (list == null || list.Count <= 1)
                return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomInt(0, i, true, channel);
                (list[j], list[i]) = (list[i], list[j]);
            }
        }

        public static T GetRandomWeightedElement<T>(
            IList<T> elements,
            IList<float> weights,
            RandomChannel channel = RandomChannel.Global)
        {
            if (elements == null || weights == null)
                return default;

            if (elements.Count == 0 || elements.Count != weights.Count)
                return default;

            float totalWeight = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > 0f)
                    totalWeight += weights[i];
            }

            if (totalWeight <= 0f)
                return default;

            float randomValue = RandomFloat(0f, totalWeight, channel);

            for (int i = 0; i < elements.Count; i++)
            {
                float weight = weights[i];

                if (weight <= 0f)
                    continue;

                if (randomValue < weight)
                    return elements[i];

                randomValue -= weight;
            }

            return elements[elements.Count - 1];
        }

        public static T GetRandomWeightedElement<T>(
            IList<T> elements,
            Func<T, float> getWeight,
            RandomChannel channel = RandomChannel.Global)
        {
            if (elements == null || elements.Count == 0 || getWeight == null)
                return default;

            float totalWeight = 0f;

            for (int i = 0; i < elements.Count; i++)
            {
                float weight = getWeight.Invoke(elements[i]);

                if (weight > 0f)
                    totalWeight += weight;
            }

            if (totalWeight <= 0f)
                return default;

            float randomValue = RandomFloat(0f, totalWeight, channel);

            for (int i = 0; i < elements.Count; i++)
            {
                float weight = getWeight.Invoke(elements[i]);

                if (weight <= 0f)
                    continue;

                if (randomValue < weight)
                    return elements[i];

                randomValue -= weight;
            }

            return elements[elements.Count - 1];
        }

        public static List<T> GetRandomWeightedMultiSelect<T>(
            IList<T> elements,
            IList<float> weights,
            int selectionCount,
            RandomChannel channel = RandomChannel.Global)
        {
            List<T> selectedItems = new();

            if (elements == null || weights == null)
                return selectedItems;

            if (elements.Count == 0 || elements.Count != weights.Count || selectionCount <= 0)
                return selectedItems;

            selectionCount = Math.Min(selectionCount, elements.Count);

            List<float> tempWeights = new(weights);

            for (int i = 0; i < selectionCount; i++)
            {
                float totalWeight = 0f;

                for (int j = 0; j < tempWeights.Count; j++)
                {
                    if (tempWeights[j] > 0f)
                        totalWeight += tempWeights[j];
                }

                if (totalWeight <= 0f)
                    break;

                float randomValue = RandomFloat(0f, totalWeight, channel);

                for (int j = 0; j < elements.Count; j++)
                {
                    float weight = tempWeights[j];

                    if (weight <= 0f)
                        continue;

                    if (randomValue < weight)
                    {
                        selectedItems.Add(elements[j]);
                        tempWeights[j] = 0f;
                        break;
                    }

                    randomValue -= weight;
                }
            }

            return selectedItems;
        }

        public static T GetRandomWeightedChance<T>(
            Dictionary<T, float> weightedOptions,
            RandomChannel channel = RandomChannel.Global)
        {
            if (weightedOptions == null || weightedOptions.Count == 0)
                return default;

            float totalWeight = 0f;

            foreach (float weight in weightedOptions.Values)
            {
                if (weight > 0f)
                    totalWeight += weight;
            }

            if (totalWeight <= 0f)
                return default;

            float randomValue = RandomFloat(0f, totalWeight, channel);

            foreach (var option in weightedOptions)
            {
                float weight = option.Value;

                if (weight <= 0f)
                    continue;

                if (randomValue < weight)
                    return option.Key;

                randomValue -= weight;
            }

            return default;
        }

        public static List<T> GetRandomMultiSelect<T>(
            IList<T> elements,
            IList<float> probabilities,
            RandomChannel channel = RandomChannel.Global)
        {
            List<T> selectedItems = new();

            if (elements == null || probabilities == null)
                return selectedItems;

            int count = Math.Min(elements.Count, probabilities.Count);

            for (int i = 0; i < count; i++)
            {
                if (RandomChance(probabilities[i], channel))
                    selectedItems.Add(elements[i]);
            }

            return selectedItems;
        }

        public static float[] GenerateRandomWeights(
            int count,
            float min,
            float max,
            RandomChannel channel = RandomChannel.Global)
        {
            if (count <= 0)
                return Array.Empty<float>();

            if (max < min)
                (min, max) = (max, min);

            float[] rawValues = new float[count];
            float sum = 0f;

            for (int i = 0; i < count; i++)
            {
                rawValues[i] = RandomFloat(min, max, channel);

                if (rawValues[i] > 0f)
                    sum += rawValues[i];
            }

            if (sum <= 0f)
            {
                float average = 1f / count;

                for (int i = 0; i < count; i++)
                {
                    rawValues[i] = average;
                }

                return rawValues;
            }

            float[] normalizedWeights = new float[count];

            for (int i = 0; i < count; i++)
            {
                normalizedWeights[i] = rawValues[i] / sum;
            }

            return normalizedWeights;
        }

        public static float RandomAngle(float minAngle = 0f, float maxAngle = 360f, RandomChannel channel = RandomChannel.Global)
        {
            return RandomFloat(minAngle, maxAngle, channel);
        }

        public static (float x, float y) RandomPointInCircle(float radius, RandomChannel channel = RandomChannel.Global)
        {
            if (radius <= 0f)
                return (0f, 0f);

            double angle = RandomFloat(0f, (float)Math.PI * 2f, channel);
            double distance = Math.Sqrt(RandomFloat(channel)) * radius;

            return (
                x: (float)(Math.Cos(angle) * distance),
                y: (float)(Math.Sin(angle) * distance)
            );
        }

        public static (float x, float y, float z) RandomPointInSphere(float radius, RandomChannel channel = RandomChannel.Global)
        {
            if (radius <= 0f)
                return (0f, 0f, 0f);

            double u = RandomFloat(channel);
            double v = RandomFloat(channel);

            double theta = 2.0 * Math.PI * u;
            double cosPhi = 2.0 * v - 1.0;
            double sinPhi = Math.Sqrt(1.0 - cosPhi * cosPhi);
            double r = Math.Pow(RandomFloat(channel), 1.0 / 3.0) * radius;

            float x = (float)(r * sinPhi * Math.Cos(theta));
            float y = (float)(r * sinPhi * Math.Sin(theta));
            float z = (float)(r * cosPhi);

            return (x, y, z);
        }

        public static List<int> GetUniqueRandomNumbers(
            int count,
            int min,
            int max,
            bool includeMax = false,
            RandomChannel channel = RandomChannel.Global)
        {
            List<int> result = new();

            int realMax = includeMax ? max + 1 : max;

            if (realMax <= min || count <= 0)
                return result;

            int rangeCount = realMax - min;
            count = Math.Min(count, rangeCount);

            List<int> numbers = new(rangeCount);

            for (int i = min; i < realMax; i++)
            {
                numbers.Add(i);
            }

            for (int i = 0; i < count; i++)
            {
                int randomIndex = RandomInt(0, numbers.Count, false, channel);
                int value = numbers[randomIndex];

                result.Add(value);

                int lastIndex = numbers.Count - 1;
                numbers[randomIndex] = numbers[lastIndex];
                numbers.RemoveAt(lastIndex);
            }

            return result;
        }

        /// <summary>
        /// 注意：这里的 variance 实际上按标准差使用。
        /// </summary>
        public static float GenerateGaussian(
            float mean,
            float variance,
            float min,
            float max,
            RandomChannel channel = RandomChannel.Global)
        {
            if (max < min)
                (min, max) = (max, min);

            float x;
            int attempts = 0;

            do
            {
                float u1 = 1.0f - RandomFloat(channel);
                float u2 = 1.0f - RandomFloat(channel);

                float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));

                x = mean + variance * randStdNormal;
                attempts++;

                if (attempts >= GaussianMaxAttempts)
                    return Math.Clamp(x, min, max);

            } while (x < min || x > max);

            return x;
        }

        public static string RandomString(int length, RandomChannel channel = RandomChannel.Global)
        {
            if (length <= 0)
                return string.Empty;

            char[] stringChars = new char[length];
            Random random = GetRandom(channel);

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = CharContents[random.Next(CharContents.Length)];
            }

            return new string(stringChars);
        }

        public static List<char> RandomCharacters(
            int count,
            string chars = CharContents,
            RandomChannel channel = RandomChannel.Global)
        {
            List<char> characterList = new();

            if (count <= 0 || string.IsNullOrEmpty(chars))
                return characterList;

            Random random = GetRandom(channel);

            for (int i = 0; i < count; i++)
            {
                characterList.Add(chars[random.Next(chars.Length)]);
            }

            return characterList;
        }

        public static (float r, float g, float b) RandomRGBColor(RandomChannel channel = RandomChannel.Global)
        {
            return (
                RandomFloat(0f, 1f, channel),
                RandomFloat(0f, 1f, channel),
                RandomFloat(0f, 1f, channel)
            );
        }

        public static (float r, float g, float b, float a) RandomRGBAColor(RandomChannel channel = RandomChannel.Global)
        {
            return (
                RandomFloat(0f, 1f, channel),
                RandomFloat(0f, 1f, channel),
                RandomFloat(0f, 1f, channel),
                RandomFloat(0f, 1f, channel)
            );
        }

        public static (float x, float y) RandomVector2(
            float minX,
            float maxX,
            float minY,
            float maxY,
            RandomChannel channel = RandomChannel.Global)
        {
            return (
                RandomFloat(minX, maxX, channel),
                RandomFloat(minY, maxY, channel)
            );
        }

        public static (float x, float y, float z) RandomVector3(
            float minX,
            float maxX,
            float minY,
            float maxY,
            float minZ,
            float maxZ,
            RandomChannel channel = RandomChannel.Global)
        {
            return (
                RandomFloat(minX, maxX, channel),
                RandomFloat(minY, maxY, channel),
                RandomFloat(minZ, maxZ, channel)
            );
        }

        public static (float x, float y) GetRandomDirection2D(RandomChannel channel = RandomChannel.Global)
        {
            float angle = RandomAngle(0f, 360f, channel);
            float rad = angle * MathF.PI / 180f;

            return (
                (float)Math.Cos(rad),
                (float)Math.Sin(rad)
            );
        }
    }
}