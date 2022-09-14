namespace csDelaunay.Delaunay;

// Also know as heap
public class HalfedgePriorityQueue
{
    private readonly int hashSize;
    private readonly Halfedge[] hash;
    private readonly float ymin;
    private readonly float deltaY;

    private int count;
    private int minBucked;

    public HalfedgePriorityQueue(float ymin, float deltaY, int sqrtSitesNb)
    {
        this.ymin = ymin;
        this.deltaY = deltaY;
        hashSize = 4 * sqrtSitesNb;
        count = 0;
        minBucked = 0;
        hash = new Halfedge[hashSize];
        // Dummy Halfedge at the top of each hash
        for (var i = 0; i < hashSize; i++)
        {
            hash[i] = Halfedge.CreateDummy();
            hash[i].NextInPriorityQueue = null;
        }
    }

    public void Dispose()
    {
        // Get rid of dummies
        for (var i = 0; i < hashSize; i++)
        {
            hash[i].Dispose();
        }
    }

    public void Insert(Halfedge halfedge)
    {
        Halfedge previous, next;

        var insertionBucket = Bucket(halfedge);
        if (insertionBucket < minBucked)
        {
            minBucked = insertionBucket;
        }
        previous = hash[insertionBucket];
        while ((next = previous.NextInPriorityQueue) != null &&
               (halfedge.YStar > next.YStar || halfedge.YStar == next.YStar && halfedge.Vertex.x > next.Vertex.x))
        {
            previous = next;
        }
        halfedge.NextInPriorityQueue = previous.NextInPriorityQueue;
        previous.NextInPriorityQueue = halfedge;
        count++;
    }

    public void Remove(Halfedge halfedge)
    {
        Halfedge previous;
        var removalBucket = Bucket(halfedge);

        if (halfedge.Vertex != null)
        {
            previous = hash[removalBucket];
            while (previous.NextInPriorityQueue != halfedge)
            {
                previous = previous.NextInPriorityQueue;
            }
            previous.NextInPriorityQueue = halfedge.NextInPriorityQueue;
            count--;
            halfedge.Vertex = null;
            halfedge.NextInPriorityQueue = null;
            halfedge.Dispose();
        }
    }

    public bool Empty()
    {
        return count == 0;
    }

    public Vector2 Min()
    {
        AdjustMinBucket();
        var answer = hash[minBucked].NextInPriorityQueue;
        return new Vector2(answer.Vertex.x, answer.YStar);
    }

    public Halfedge ExtractMin()
    {
        Halfedge answer;

        // Get the first real Halfedge in minBucket
        answer = hash[minBucked].NextInPriorityQueue;

        hash[minBucked].NextInPriorityQueue = answer.NextInPriorityQueue;
        count--;
        answer.NextInPriorityQueue = null;

        return answer;
    }

    private int Bucket(Halfedge halfedge)
    {
        var theBucket = (int)((halfedge.YStar - ymin) / deltaY * hashSize);
        if (theBucket < 0) theBucket = 0;
        if (theBucket >= hashSize) theBucket = hashSize - 1;
        return theBucket;
    }

    private bool IsEmpty(int bucket)
    {
        return hash[bucket].NextInPriorityQueue == null;
    }

    /*
         * move minBucket until it contains an actual Halfedge (not just the dummy at the top);
         */

    private void AdjustMinBucket()
    {
        while (minBucked < hashSize - 1 && IsEmpty(minBucked))
        {
            minBucked++;
        }
    }

    /*
         * @return coordinates of the Halfedge's vertex in V*, the transformed Voronoi diagram
         */
    /*
         * Remove and return the min Halfedge
         */
}
