using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Mathematics.Utilities
{
    public class AccelerationHelper
    {
        private double _calcOldDist = 0;
        private double _calcOldTime = 0;
        private double _calcNewDist = 0;
        private double _calcNewTime = 0;

        //mm/sek²
        public double Acceleration { get; set; }
        //mm/sek²
        public double Deceleration { get; set; }
        //mm/sek
        public double Speed { get; set; }
        //mm
        public double Distance { get; set; }

        //mm
        double _accelDist;
        double _accelTime;
        //mm
        double _constDist;
        double _constTime;
        //mm
        double _decelDist;
        double _decelTime;

        double _maxSpeed;

        public void Init()
        {
            _calcOldDist = 0;
            _calcNewDist = 0;
            _calcOldTime = 0;
            _calcNewTime = 0;
        }

        public void Calc()
        {

            lock (this)
            {
                this.Init();

                _constDist = 0;
                _constTime = 0;

                //s=v²/2a
                _accelDist = (Speed * Speed) / (2 * Acceleration);
                _accelTime = Speed / Acceleration;

                _decelDist = (Speed * Speed) / (2 * Deceleration);
                _decelTime = Speed / Deceleration;

                _maxSpeed = Speed;

                if ((_accelDist + _decelDist) > Distance)
                {
                    _accelTime = (double)Math.Pow(Distance / (Acceleration * (0.5 + 0.5 * Acceleration / Deceleration)), 0.5);
                    _decelTime = _accelTime * Acceleration / Deceleration;
                    _accelDist = (Acceleration / 2) * (_accelTime * _accelTime);
                    _decelDist = (Deceleration / 2) * (_decelTime * _decelTime);

                    _maxSpeed = Acceleration * _accelTime;

                }
                else
                {
                    _constDist = Distance - _accelDist - _decelDist;
                    _constTime = _constDist / Speed;
                }
            }
        }


        public double AddPathAndGetNextEventTime(double dist)
        {
            lock (this)
            {
                double retTime = 0;
                bool calculated = false;


                _calcOldDist = _calcNewDist;
                _calcOldTime = _calcNewTime;

                _calcNewDist += dist;

                double tmpDist = _calcNewDist;

                //Acceleration -----------------------------------
                if ((_calcNewDist <= _accelDist) && calculated == false)
                {
                    _calcNewTime = (double)Math.Sqrt(2 * tmpDist / Acceleration);

                    retTime = _calcNewTime - _calcOldTime;
                    calculated = true;
                }

                //ConstantPhase ----------------------------------
                if ((_calcNewDist <= (_accelDist + _constDist)) && calculated == false)
                {

                    _calcNewTime = _accelTime;

                    tmpDist -= _accelDist;// remove accelDist 
                    _calcNewTime += tmpDist / Speed;// Now add const time 

                    retTime = _calcNewTime - _calcOldTime;
                    calculated = true;
                }

                // Decceleration-----------------------------------
                // I use distance because in fact of double we can run in troubles 
                if (calculated == false)
                {

                    tmpDist -= _accelDist;
                    tmpDist -= _constDist;

                    _calcNewTime = _accelTime;
                    _calcNewTime += _constTime;

                    //Calc inverse 
                    _calcNewTime += _decelTime;

                    double x = (2 * (_decelDist - tmpDist) / (Deceleration));
                    if (x > 0)
                    {
                        _calcNewTime -= (double)Math.Sqrt(x);
                    }
                    retTime = _calcNewTime - _calcOldTime;
                    calculated = true;
                }

                if (retTime <= 0)
                {
                    throw new NotSupportedException("0 Time not allowed");
                }
                else
                {
                    return retTime;
                }
            }
        }

        // returns with 0 to 1 
        public double GetCompletedPathFractionForCurSegment(double elapsedTime)
        {
            lock (this)
            {
                double completed = 0;
                bool calculated = false;

                double startTime = _calcOldTime;
                double startDist = _calcOldDist;

                double tmpTime = startTime + elapsedTime;
                double tmpDist = 0;

                //AccelerationPhase-----------------------------------
                if ((tmpTime <= _accelTime) && calculated == false)
                {
                    tmpDist = (Acceleration / 2) * (tmpTime * tmpTime);
                    completed = (tmpDist - _calcOldDist) / (_calcNewDist - _calcOldDist);
                    calculated = true;
                }

                //ConstantPhase --------------------------------------
                if ((tmpTime <= _accelTime + _constTime) && calculated == false)
                {
                    tmpDist += _accelDist;
                    tmpTime -= _accelTime;

                    tmpDist += _maxSpeed * tmpTime;

                    completed = (tmpDist - _calcOldDist) / (_calcNewDist - _calcOldDist);
                    calculated = true;
                }

                //Decceleration --------------------------------------
                if ((tmpTime <= _accelTime + _constTime + _decelTime) && calculated == false)
                {

                    tmpDist += _accelDist;
                    tmpTime -= _accelTime;

                    tmpDist += _constDist;
                    tmpTime -= _constTime;

                    //Calc inverse 
                    tmpDist += _decelDist;
                    tmpDist -= (Deceleration / 2) * ((_decelTime - tmpTime) * (_decelTime - tmpTime));

                    completed = (tmpDist - _calcOldDist) / (_calcNewDist - _calcOldDist);
                    calculated = true;
                }

                //FullPath
                if (completed > 1) completed = 1;
                if (completed < 0) completed = 0;
                return completed;
            }
        }
    }
}