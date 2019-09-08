
		$(document).ready(function(){

			InitializeSelectLists();
			//alert('done initializing jquery code');

			$("#RunChartButton").click(function() {
				CreateChart();
			});
			$("#RunCSVButton").click(function() {
				CreateCSV();
			});
			$("#ClearChartButton").click(function() {
				ClearChart();
			});
			$(window).resize(function(){
				ResizeCanvas();
			});
			ResizeCanvas();
		});

		function ResizeCanvas()
		{
			var canvasCtx = $("#DeviceChartResultCanvas").get(0).getContext("2d");
			if (canvasCtx != null)
				ResizeCanvas(canvasCtx);
		}
		function ResizeCanvas(canvasCtx)
		{
			//alert("resize chart entry");
		    //var varHeight = $("html").height() - $("#DeviceChartQueryDiv").height() - $("#TopDiv").height();
		    //$("#configTabs").css("height", newHeight);

			var varHeight = $(window).height() - $("#DeviceChartQueryDiv").height() - $("#TopDiv").height() - $("#contentTabs").height() - 200;
			//var varWidth = $(window).width() - $("#DeviceChartResultCanvas").offset().left;

			//alert("Resized Height: " + varHeight + "Width: " + varWidth);
			if(varHeight < 500)
				varHeight = 500;

			//alert(varHeight);

			//canvasCtx.canvas.height = varHeight;
			//canvasCtx.canvas.width = varWidth;
			//canvasCtx.canvas.height(varHeight);
			//var chartCanvas = $("#DeviceChartResultCanvas").get(0);
			//chartCanvas.height(varHeight);
			//chartCanvas.width($("#DeviceChartResultCanvas").width() - 20);
			//alert(chartCanvas.height());
		}

		function InitializeSelectLists()
		{
			if (deviceMapData == null)
			{
				$.getJSON("ListDevices.json", function(data) {
					deviceMapData = data;
					$.each(deviceMapData, function(Index, DeviceMap){
						
						//alert(DeviceMap.DeviceType);
						deviceTypeData.push(DeviceMap.DeviceType);
						//deviceMapData.push(DeviceMap.Name);
					});
					deviceTypeData = $.unique(deviceTypeData);
					//alert('Device Type Count: ' + deviceTypeData.length);
					LoadDeviceMapData();
				});
			}
			else
			{
					LoadDeviceMapData();
			}
			
		}
		function LoadDeviceMapData()
		{
			deviceMapOptionHtml = '';
			deviceTypeOptionHtml = '';

			$.each(deviceTypeData, function(Index, DeviceType){
				//alert(DeviceType);
				deviceTypeOptionHtml += "<option value='" + DeviceType + "'>" + DeviceType + "</option>";
			});

			$.each(deviceMapData, function(Index, DeviceMap){
				//alert("Device: " + Name);
				deviceMapOptionHtml += "<option value='" + Index + "'>" + DeviceMap.Name + "</option>";
			});

			//alert(deviceTypeOptionHtml);
			//alert(deviceMapOptionHtml);

			$("#DeviceTypeSelectList").html(deviceTypeOptionHtml);
			$("#DeviceMapSelectList").html(deviceMapOptionHtml);

			$("#DeviceTypeSelectList").multiselect({
				header: "Select a device type", 
				selectedText: "# Selected"
			});
			$("#DeviceMapSelectList").multiselect({
				header: "Select a device", 
				selectedText: "# Selected"
			});
		}

		function CreateChart()
		{
			return RunChart(false);
		}
		function CreateCSV()
		{
			return RunChart(true);
		}
		function RunChart(toCSV)
		{
			$("#ChartLoadingWaitDiv").show();
			//Parse the selected options from the filter criteria.
			//alert('Parsing query values');
			var queryParams = '';

			//Assemble the parameters for the request and submit.
			var mapVal = $("#DeviceMapSelectList").val();
			var typeVal = $("#DeviceTypeSelectList").val();

			queryParams += "start=" + $("#startDate").datepicker({dateFormat: 'yyyy/MM/dd'}).val() + "T00:00:00.000&";
			queryParams += "end=" + $("#endDate").datepicker({dateFormat: 'yyyy/MM/dd'}).val() + "T23:59:59.999&";

			if (toCSV == true)
			{
				queryParams += "csv=true&";
			}

			if (mapVal != null && mapVal.length > 0)
			{
				//alert("Map values are being set " + mapVal);
			
				$.each(mapVal, function(Index, mapItem){
					queryParams += "name=" + deviceMapData[mapItem].Key + "&";
				});
			}

			if (typeVal != null && typeVal.length > 0)
			{
				//alert("Type values are being set " + typeVal);
				$.each(typeVal, function(Index, typeItem){
					queryParams += "type=" + typeItem + "&";
				});
			}

			//var uvChartObj = uv.chart('Line', graphdef, options);
			//var uvChartObj = uv.chart('Line', testGraphData, options);
			Chart.defaults.global.tooltipFontSize = 12;
			Chart.defaults.global.tooltipTemplate = "<%if (label){%><%=label%>: <%}%><%= value %>";
			
			Chart.defaults.global.animation = false;
			Chart.defaults.global.showScale= true;

			Chart.defaults.global.scaleStepWidth = 2;
			Chart.defaults.global.scaleLineWidth = 2;

		    Chart.defaults.global.pointHitDetectionRadius = 2;
		    Chart.defaults.global.responsive = true;
		    Chart.defaults.global.maintainAspectRatio = false;
		    Chart.defaults.global.showToolTips = true;
		    Chart.defaults.global.scaleShowLabels = false;
			//Chart.defaults.global.legendTemplate = "<ul class=\"<%=name.toLowerCase()%>-legend\"><% for (var i=0; i<datasets.length; i++){%><li><span style=\"background-color:<%=datasets[i].strokeColor%>\"></span><%if(datasets[i].label){%><%=dataset[i].label%><%}%></li><%}%></ul>";
			

			//alert("Retrieving with url DeviceQuery.json?" + queryParams);
			if (toCSV)
			{
				$("#CSVDownloadFrame").attr("src","DeviceQuery.json?" + queryParams);
			}
			else
			{
				$.getJSON("DeviceQuery.json?" + queryParams, function(returnData) {

					//alert("Creating chart");
					//var uvChartDa = uv.Chart("Line", returnData, options);
					//alert("creating canvas in DeviceChartResultDiv");

					var height = $(window).height() - $("#DeviceChartResultDiv").offset().top;

					//alert("canvas height:" + height);
					$("#DeviceChartResultDiv").html("<canvas id=\"DeviceChartResultCanvas\" width=1024 height=" + height + "></canvas>");
					//alert("Creating chart");
					//alert($("#DeviceChartResultDiv").html());
					var canvas = $("#DeviceChartResultCanvas").get(0);
					var ctx = canvas.getContext("2d");
					//ctx.save();

					//ctx.setTransform(1,0,0,1,0,0);
					//ctx.clearRect(0,0,canvas.width, canvas.height);

					//ctx.restore();
					//ctx.canvas.width = canvas.width;
					//ctx.canvas.height = canvas.height;

					var options = {
						tooltipTemplate : "<%= label %>: <%}%><%= value %>", //"<%if (label){%><%=label%>: <%}%><%= value %>"
						legendTemplate : "<ul class=\"<%=name.toLowerCase()%>-legend\"><% for (var i=0; i<datasets.length; i++){%><li><span style=\"background-color:<%=datasets[i].strokeColor%>\"></span><%if(datasets[i].label){%><%=datasets[i].label%><%}%></li><%}%></ul>",
    					multiTooltipTemplate: "<%if (datasetLabel){%><%=datasetLabel%>: <%}%><%= value %>",
		    			responsive: true,
		    			maintainAspectRatio: false,
		    			showToolTips: true,
		   				scaleShowLabels: true,
		   				barShowLabels: false
					}

					ResizeCanvas(ctx);
					//alert("Context created");
					var resultChart = new Chart(ctx).Line(returnData, options);
					//ctx.save();
					//var resultChart = new Chart(ctx).Line(data);
					//alert("Created chart");
				});
			}


			$("#ChartLoadingWaitDiv").hide();
		}
		function ClearChart()
		{
			$("#DeviceChartResultDiv").html("");
		}