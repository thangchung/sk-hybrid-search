using System.Numerics.Tensors;

namespace HydeSearch.Services;

/// <summary>
/// Interface for calculating vector similarities
/// </summary>
public interface IVectorSimilarityService
{
    float CalculateCosineSimilarity(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2);
    float CalculateDotProduct(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2);
    float CalculateEuclideanDistance(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2);
}

/// <summary>
/// High-performance vector similarity calculations using System.Numerics.Tensors
/// </summary>
public class VectorSimilarityService : IVectorSimilarityService
{
    public float CalculateCosineSimilarity(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vectors must have the same dimension");

        var dotProduct = TensorPrimitives.Dot(vector1, vector2);
        var magnitude1 = Math.Sqrt(TensorPrimitives.Dot(vector1, vector1));
        var magnitude2 = Math.Sqrt(TensorPrimitives.Dot(vector2, vector2));

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return (float)(dotProduct / (magnitude1 * magnitude2));
    }

    public float CalculateDotProduct(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vectors must have the same dimension");

        return TensorPrimitives.Dot(vector1, vector2);
    }

    public float CalculateEuclideanDistance(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vectors must have the same dimension");

        Span<float> difference = stackalloc float[vector1.Length];
        TensorPrimitives.Subtract(vector1, vector2, difference);
        
        return (float)Math.Sqrt(TensorPrimitives.Dot(difference, difference));
    }
}
