# Unity-LIDAR-Compute
A weekend experiment to visualize LIDAR data from http://environment.data.gov.uk/ds/survey/index.jsp 

NOTE: To run this, you have to extract the data.zip file in the project folder such that you get LIDAR-* folders in the project root next to assets folder.

This was a weekend study that was supposed to turn into a complete LIDAR rendering framework but was cut short as my laptop died.

The rendering is done via compute shaders. We read all the LIDAR data into memory and then copy the LIDAR data into compute buffers which do the tile based transformation and then pass the data to a geometry shader which generates the quads on each point.

There is a frustrum culling optimization that is in place but not being applied as it has a bug.

I am using append buffers and DrawProceduralIndirect to avoid having any need for a read back.

Provided as is and hope it helps someone.

Cheers!
