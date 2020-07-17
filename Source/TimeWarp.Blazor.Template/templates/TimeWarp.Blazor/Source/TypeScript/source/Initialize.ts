import { TimeWarpBlazor } from './TimeWarpBlazor';

var _TimeWarpBlazor_: TimeWarpBlazor;

function InitializeTimeWarpBlazor() {
  console.log("Initialize TimeWarpBlazor");
  if (_TimeWarpBlazor_ === undefined) {
    console.log("creating new");
    _TimeWarpBlazor_ = new TimeWarpBlazor();
    console.log("created new");
  }
  console.log("wtf");
}

InitializeTimeWarpBlazor();