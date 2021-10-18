import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../D3Scripts/Components/ChartUtils';
import * as AppContext from '@framework/AppContext';
import { useAPI } from '../../../Signum.React/Scripts/Hooks';
import { scaleFor } from '../D3Scripts/Components/ChartUtils';
import { MemoRepository } from '../D3Scripts/Components/ReactChart';


export default function renderSvgMap(p: ChartClient.ChartScriptProps) {

  return <SvgMap {...p} />
}

function SvgMap({ data, parameters, onDrillDown, width, height, chartRequest }: ChartClient.ChartScriptProps) {

  const memo = React.useMemo(() => new MemoRepository(), [chartRequest]);

  const divRef = React.useRef<HTMLDivElement>(null);


  var svgUrl = parameters["SvgUrl"];
  var locationSelector = parameters["LocationSelector"];
  var locationAttribute = parameters["LocationAttribute"];
  var locationMatch = parameters["LocationMatch"];
  var strokeColor = parameters["StrokeColor"];
  var strokeWidth = parameters["StrokeWidth"];
  var opacityScale = parameters["OpacityScale"];
  var colorScaleMaxValue = parameters["ColorScaleMaxValue"];
  var colorScale = parameters["ColorScale"];
  var colorInterpolate = parameters["ColorInterpolate"]

  var svgBox = useAPI(() => fetch(AppContext.toAbsoluteUrl(svgUrl)).then(r => r.text()).then(svgText => ({ svgText })), [svgUrl]);

  var refOnDrillDown = React.useRef<(attr: string) => void>();

  React.useEffect(() => {
    if (svgBox) {
      divRef.current!.innerHTML = svgBox.svgText;
      var svgElement = divRef.current!.getElementsByTagName("svg")[0] as SVGElement;
      svgElement.removeAttribute("height");
      svgElement.removeAttribute("width");
    }
  }, [svgBox]);

  React.useEffect(() => {
    var svgElement = divRef.current!.getElementsByTagName("svg")[0] as SVGElement;
    if (svgElement) {
      svgElement.style.maxWidth = width + "px";
      svgElement.style.maxHeight = height + "px";
    }
  }, [width, height, svgBox]);

  React.useEffect(() => {

    var svg = divRef.current!.getElementsByTagName("svg")[0] as SVGElement;
    if (data != null && svg != null) {
      var locationCodeColumn = data.columns.c0! as ChartClient.ChartColumn<string>;
      var locationColumn = data.columns.c1;
      var colorScaleColumn = data.columns.c2 as ChartClient.ChartColumn<number>;
      var colorCategoryColumn = data.columns.c3;
      var opacityColumn = data.columns.c4 as ChartClient.ChartColumn<number> | undefined;

      var opacity: null | ((row: number) => number | undefined) = null;
      if (opacityColumn != null) {
        opacity = scaleFor(opacityColumn, data.rows.map(opacityColumn.getValue), 0, 1, opacityScale);
      }

      var color: (r: ChartClient.ChartRow) => string | undefined;
      if (colorScaleColumn) {
        var values = data.rows.map(r => colorScaleColumn!.getValue(r));
  
        if (colorScaleMaxValue)
          values.push(parseFloat(colorScaleMaxValue));

        var scaleFunc = scaleFor(colorScaleColumn, values, 0, 1, colorScale);
        var colorInterpolator = ChartUtils.getColorInterpolation(colorInterpolate);
        color = r => colorInterpolator && colorInterpolator(scaleFunc(colorScaleColumn!.getValue(r))!);
      }
      else if (colorCategoryColumn) {
        var categoryColor = ChartUtils.colorCategory(parameters, data.rows.map(r => colorCategoryColumn!.getValueKey(r)), memo);
        color = r => colorCategoryColumn!.getColor(r) ?? categoryColor(colorCategoryColumn!.getValueKey(r));
      } else {
        color = r => "gray";
      }

      var dataDic = data.rows.toObject(row => locationCodeColumn.getValue(row));

      var getRow: (attr: string) => ChartClient.ChartRow | undefined;

      if (locationMatch == "Exact")
        getRow = attr => dataDic[attr];
      else {
        var lengths = Object.keys(dataDic).map(a => a.length).distinctBy(a => a.toString())
          .orderByDescending(a => a);

        if (lengths.length == 1) {
          var onlyLen = lengths[0];
          getRow = attr => dataDic[attr.substring(0, onlyLen)];
        }
        else
          getRow = attr => {
            for (var i = 0; i < lengths.length; i++) {
              var len = lengths[i];
              if (len <= attr.length) {
                var res = dataDic[attr.substring(0, len)];
                if (res != null)
                  return res;
              }
            }
            return undefined;
          };
      }

      var svg = divRef.current!.getElementsByTagName("svg")[0] as SVGElement;
      svg.querySelectorAll<SVGPathElement>(locationSelector).forEach(elem => {
        var attr = elem.getAttribute(locationAttribute);

        if (attr == null)
          return;

        var row = getRow(attr);

        if (strokeWidth)
          elem.setAttribute("stroke-width", strokeWidth);
        else
          elem.removeAttribute("stoke-width");

        if (strokeColor)
          elem.setAttribute("stroke", strokeColor);
        else
          elem.removeAttribute("stroke");

        var titleElement = elem.querySelector("title") ?? elem.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "title"));

        if (row) {
          elem.setAttribute("fill", color(row)!);
          elem.setAttribute("cursor", "pointer");

          titleElement.textContent =
            (locationColumn == null ? locationCodeColumn.getValueNiceName(row) : locationColumn.getValueNiceName(row))
            + (colorScaleColumn == null ? '' : (' (' + colorScaleColumn.getValueNiceName(row) + ')'))
            + (colorCategoryColumn == null ? '' : (' (' + colorCategoryColumn.getValueNiceName(row) + ')'))
            + (opacityColumn == null ? '' : (' (' + opacityColumn.getValueNiceName(row) + ')'));
        }
        else {
          elem.removeAttribute("fill");
          elem.removeAttribute("cursor");
        }
      });

      var onClick = (e: MouseEvent) => {
        if ((e.target as SVGElement).matches(locationSelector)) {
          e.preventDefault();
          var attr = (e.target as SVGElement).getAttribute(locationAttribute);
          if (attr == null)
            return;

          var row = getRow(attr);
          if (row == null)
            return;

          onDrillDown(row, e);
        }
      };

      svg.addEventListener("click", onClick);

      return () => svg.removeEventListener("click", onClick);
    }

  }, [svgBox, data, locationSelector, locationAttribute, locationMatch, strokeColor, strokeWidth, opacityScale, colorScaleMaxValue, colorScale, colorInterpolate]);
  
  return (
    <div ref={divRef} style={{ imageRendering: "-webkit-optimize-contrast", userSelect: "none", display: "flex", justifyContent: "center" }}>

    </div>
  );
}
