using System;
using System.Collections.Generic;
//using System.Linq;

namespace UnknownCreator.Modules
{
    public static class RVUtils
    {
        public static int GaussianMaxAttempts = 100;

        private const string CharContents = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private static Random _random;

        static RVUtils()
        {
            _random = new Random();
        }

        // ���������������
        public static void SetSeed(int seed)
        {
            _random = new Random(seed);
        }

        // ������������ɷ���
        public static int RandomInt(int min, int max, bool IncludeMax)
        {
            return _random.Next(min, IncludeMax ? max + 1 : max);
        }
        public static float RandomFloat(float min, float max, bool includeMax)
        {
            float adjustedMax = includeMax ? max + float.Epsilon : max;
            return (float)(_random.NextDouble() * (adjustedMax - min) + min);
        }
        public static float RandomFloat()
        {
            return (float)_random.NextDouble();
        }

        // �������ֵ
        public static bool RandomBool()
        {
            return _random.Next(0, 2) == 0;
        }

        // ����һ�������µĽ��
        public static bool RandomChance(float chance)
        {
            return RandomFloat() <= chance;
        }

        // �ٷֱȸ���
        public static bool PercChance(float percentage)
        {
            return RandomFloat(0f, 100f, true) < Math.Clamp(percentage, 0f, 100f);
        }
        public static bool PercChance(double percentage)
        {
            return RandomFloat(0f, 100f, true) < Math.Clamp(percentage, 0f, 100f);
        }

        // �������������ȡһ��Ԫ��
        public static T RandomElement<T>(T[] array)
        {
            return array[RandomInt(0, array.Length, false)];
        }

        // ���б��������ȡһ��Ԫ��
        public static T RandomElement<T>(List<T> list)
        {
            return list[RandomInt(0, list.Count, false)];
        }

        // ��һ���ֵ��������ȡһ����ֵ��
        public static KeyValuePair<TKey, TValue> RandomElement<TKey, TValue>(Dictionary<TKey, TValue> dict)
        {
            List<TKey> keys = new List<TKey>(dict.Keys);
            TKey randomKey = keys[RandomInt(0, keys.Count, false)];
            return new KeyValuePair<TKey, TValue>(randomKey, dict[randomKey]);
        }

        // �����������Ԫ��
        public static void ShuffleArray<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomInt(0, i, true);
                (array[j], array[i]) = (array[i], array[j]);
            }
        }

        // ��������б�Ԫ��
        public static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomInt(0, i, true);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        // ����һ�������µļ�Ȩѡ�����磺ϡ����Ʒ���䣩
        public static T GetRandomWeightedElement<T>(List<T> elements, List<float> weights)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += weights[i];
            }

            float randomValue = RandomFloat(0f, totalWeight, true);
            for (int i = 0; i < elements.Count; i++)
            {
                if (randomValue < weights[i])
                {
                    return elements[i];
                }
                randomValue -= weights[i];
            }
            return default;
        }

        // ����Ȩ�����ѡ����Ԫ��
        public static List<T> GetRandomWeightedMultiSelect<T>(List<T> elements, List<float> weights, int selectionCount)
        {
            List<T> selectedItems = new List<T>();
            for (int i = 0; i < selectionCount; i++)
            {
                float totalWeight = 0;
                foreach (var weight in weights)
                {
                    totalWeight += weight;
                }

                float randomValue = RandomFloat(0, totalWeight, true);
                for (int j = 0; j < elements.Count; j++)
                {
                    if (randomValue < weights[j])
                    {
                        selectedItems.Add(elements[j]);
                        weights[j] = 0; // ��ֹ�ظ�ѡ��
                        break;
                    }
                    randomValue -= weights[j];
                }
            }
            return selectedItems;
        }


        // ����һ�������µļ�Ȩѡ�����磺ϡ����Ʒ���䣩
        public static T GetRandomWeightedChance<T>(Dictionary<T, float> weightedOptions)
        {
            float totalWeight = 0;
            foreach (var option in weightedOptions.Values)
            {
                totalWeight += option;
            }

            float randomValue = RandomFloat(0, totalWeight, true);

            foreach (var option in weightedOptions)
            {
                if (randomValue < option.Value)
                    return option.Key;
                randomValue -= option.Value;
            }

            return default;
        }

        // ��һ�����ʷ��ض�������������ص���
        public static List<T> GetRandomMultiSelect<T>(List<T> elements, List<float> probabilities)
        {
            List<T> selectedItems = new List<T>();
            for (int i = 0; i < elements.Count; i++)
            {
                if (RandomChance(probabilities[i]))
                {
                    selectedItems.Add(elements[i]);
                }
            }
            return selectedItems;
        }

        public static float[] GenerateRandomWeights(int count, float min, float max)
        {
            float[] rawValues = new float[count];
            float sum = 0f;

            // ����������������ܺ�
            for (int i = 0; i < count; i++)
            {
                rawValues[i] = RandomFloat(min, max,true);
                sum += rawValues[i];
            }

            // ��һ������
            float[] normalizedWeights = new float[count];
            for (int i = 0; i < count; i++)
            {
                normalizedWeights[i] = rawValues[i] / sum;
            }

            return normalizedWeights;
        }


        // ��ȡ����Ƕ�
        public static float RandomAngle(float minAngle = 0, float maxAngle = 360)
        {
            return RandomFloat(minAngle, maxAngle, true);
        }

        // ��ȡһ����λԲ�ڵ������
        public static (float x, float y) RandomPointInCircle(float radius)
        {
            double angle = RandomFloat(0, (float)Math.PI * 2, true);
            double distance = Math.Sqrt(RandomFloat()) * radius;
            return (x: (float)(Math.Cos(angle) * distance), y: (float)(Math.Sin(angle) * distance));
        }

        // ��ȡһ����λ���ڵ������
        public static (float x, float y, float z) RandomPointInSphere(float radius)
        {
            double theta = RandomFloat(0, (float)Math.PI * 2, true);
            double phi = RandomFloat(0, (float)Math.PI, true);
            double r = RandomFloat() * radius;

            float x = (float)(r * Math.Sin(phi) * Math.Cos(theta));
            float y = (float)(r * Math.Sin(phi) * Math.Sin(theta));
            float z = (float)(r * Math.Cos(phi));

            return (x, y, z);
        }

        // ��һ����Χ��ѡ���ظ��Ķ�������
        public static List<int> GetUniqueRandomNumbers(int count, int min, int max, bool includeMax)
        {
            HashSet<int> numbers = new HashSet<int>();
            while (numbers.Count < count)
            {
                numbers.Add(RandomInt(min, max, includeMax));
            }
            return new List<int>(numbers);
        }

        // ����ָ������������ַ�
        public static float GenerateGaussian(float mean, float variance, float min, float max)
        {
            float x;
            int attempts = 0;
            do
            {
                float u1 = 1.0f - RandomFloat();
                float u2 = 1.0f - RandomFloat();
                float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
                x = mean + variance * randStdNormal;
                attempts++;

                // �����������Դ��������������ڷ�Χ�ڵ�ֵ
                if (attempts >= GaussianMaxAttempts)
                    return Math.Clamp(x, min, max);

            } while (x < min || x > max);

            return x;
        }


        // ��Ĭ���ַ�����������ַ���
        public static string RandomString(int length)
        {

            char[] stringChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                stringChars[i] = CharContents[_random.Next(CharContents.Length)];
            }
            return new string(stringChars);
        }


        // ���ݴ����ַ������ָ������������ַ�
        public static List<char> RandomCharacters(int count, string chars = CharContents)
        {
            List<char> characterList = new List<char>();
            for (int i = 0; i < count; i++)
            {
                characterList.Add(chars[_random.Next(chars.Length)]);
            }
            return characterList;
        }

        // ������� RGB ��ɫ
        public static (float r, float g, float b) RandomRGBColor()
        {
            return (RandomFloat(0f, 1f, true), RandomFloat(0f, 1f, true), RandomFloat(0f, 1f, true));
        }

        // ������� RGBA ��ɫ
        public static (float r, float g, float b, float a) RandomRGBAColor()
        {
            return (RandomFloat(0f, 1f, true), RandomFloat(0f, 1f, true), RandomFloat(0f, 1f, true), RandomFloat(0f, 1f, true));
        }

        // ��������� Vector2 ����
        public static (float x, float y) RandomVector2(float minX, float maxX, float minY, float maxY)
        {
            return (RandomFloat(minX, maxX, true), RandomFloat(minY, maxY, true));
        }

        // ��������� Vector3 ����
        public static (float x, float y, float z) RandomVector3(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            return (RandomFloat(minX, maxX, true), RandomFloat(minY, maxY, true), RandomFloat(minZ, maxZ, true));
        }

        // ����0��360�ȷ�Χ�ڵ��������
        public static (float x, float y) GetRandomDirection2D()
        {
            float angle = RandomAngle();
            return ((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }
}