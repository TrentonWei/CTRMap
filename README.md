# DorlingMap for static data and time-varying data
Two automatic algorithm for generation of a circular cartogram, namely DorlingMap, start coding~

#The Code is developed to support the findings of our submitted paper entitled “Cartogram is also a map Circular cartograms: via the elastic beam algorithm originated from cartographic generalization"，for more details see the pdf file as: waiting for our update 

#The tools were implemented in C# on ArcGIS 10.2 software (ESRI, USA). And the tool has a separate form to set the input, output and parameters for the algorithms.

(1) The DorlingMap for static data

#The principle: an elastic beam algorithm based on minimum energy principle. For more details see our up-coming paper.
![image](https://github.com/TrentonWei/DorlingMap/blob/master/Principle-1.png)

#The interface
![image](https://github.com/TrentonWei/DorlingMap/blob/master/Interface-1.png)

#The result_1:The circular cartogram generated using the proposed approach for the population of each state in the United States of America (excluding Alaska and Hawaii). 
DataSet:https://github.com/TrentonWei/DorlingMap/tree/master/Experiment%20Data%20Circular%20cartogram
![image](https://github.com/TrentonWei/DorlingMap/blob/master/USA-1.png)

#The result_2: The circular cartogram generated using the proposed approach for the population of each country in North and South America. 
Dataset:
![image](https://github.com/TrentonWei/DorlingMap/blob/master/American.png)

(2) The DorlingMap for time-varying data

#The principle: an improved elastic beam algorithm with hierarchical optimization. For more details see our up-coming paper.
![image](https://github.com/TrentonWei/DorlingMap/blob/master/Principle-2.png)

#The interface
![image](https://github.com/TrentonWei/DorlingMap/blob/master/interface-2.png)

#The result_1:The circular cartogram generated by using the proposed approach for the population of each state in the United States of America (excluding Alaska and Hawaii) from 1980 to 2015 (the time interval is 5 years, 8-time points).
Dataset:https://github.com/TrentonWei/DorlingMap/tree/master/Experiment%20Data%20for%20Efficiency%20and%20Stable%20Circular%20Cartogram
![image](https://github.com/TrentonWei/DorlingMap/blob/master/USA-2.png)

