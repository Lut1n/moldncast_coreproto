int w2 = 0;
int xi = 0;
int yi = 0;
int xi1 = 0;
int yi1 = 0;

if (yi*yi1 < 0) // ViVi1 crosses the x-axis
{
    int R = xi + yi * (xi1 - xi ) / ( yi - yi1 ); // R is the x-coordinate of the intersection of ViVi1 and the x-axis
    // if R > 0 -> positive x-axis, else negative x-axis

    if(yi * R < 0)
        // crossing from below of positive x-axis or above negative x-axis
        w2 = w2 + 2;
    else // crossing from above of positive x-axis or below negative x-axis
        w2 = w2 - 2;
}
else if (yi == 0 && yi1 == 0) // points are on the boundary
{
    // unchanged
    w2 += 0;
}
else if(yi == 0) // V1 on x-axis
{
    if(yi1 * xi > 0)
        // Vi is on positive x-axis and Vi1 above, or Vi is on negative x-axis and Vi1 below
        w2 = w2 + 1;
    else
        // Vi is on negative x-axis and Vi1 above, or Vi is on positive x-axis and Vi1 below
        w2 = w2 - 1;
}
else if(yi1 == 0) // Vi1 is on x-axis
{
    if(yi * xi1 < 0)
        // Vi1 is on negative x-axis and Vi above, or Vi1 is on positive x-axis and Vi below
        w2 = w2 + 1;
    else
        // Vi1 is on positive x-axis and Vi above, or Vi1 is on negative x-axis and Vi below
        w2 = w2 - 1;
}

return w / 2;