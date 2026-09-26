'use client'

import  {Fragment, useEffect , useState} from "react";
import { useRouter } from 'next/navigation'  
import { Dialog, DialogBackdrop, DialogPanel, DialogTitle } from '@headlessui/react' 
import { ExclamationTriangleIcon } from "@heroicons/react/24/outline";
import { extendSession } from "@/_lib/server/extend-session"; 
import {   useGetCookie } from 'cookies-next';
const eventTypes = [
  'keypress',
  'mousemove',
  'mousedown',
  'scroll',
  'touchmove',
  'pointermove'
]
export const addEventListeners = (listener:EventListenerOrEventListenerObject) => {
  eventTypes.forEach((type) => {
    window.addEventListener(type, listener, false)
  })
}
export const removeEventListeners = (listener:EventListenerOrEventListenerObject) => {
  if (listener) {
    eventTypes.forEach((type) => {
      window.removeEventListener(type, listener, false)
    })
  }
}

interface LogOff{
  (): void;
} 

const useLogOff = (): LogOff => {
    const router = useRouter()
    const logOff = ()=>{ 
        router.push('/home/logout');
    }
    return logOff;
  };

  
export  function Timeout() { 
  const getCookie = useGetCookie();
  const [isWarningModalOpen, setWarningModalOpen] = useState(false);  
  const logOffTheApp = useLogOff();  

  const continueUsingApp = (): void =>{
     setWarningModalOpen(false);
     extendSession(); 
  }

  if(!process.env.NEXT_PUBLIC_SESSION_TIMEOUT) throw new Error("NEXT_PUBLIC_SESSION_TIMEOUT env not set");
  if(!process.env.NEXT_PUBLIC_SESSION_DIALOG_TIMEOUT) throw new Error("NEXT_PUBLIC_SESSION_DIALOG_TIMEOUT env not set");
  const sessionTimeout  = parseInt(process.env.NEXT_PUBLIC_SESSION_TIMEOUT);
  const dialogTimeout =   parseInt(process.env.NEXT_PUBLIC_SESSION_DIALOG_TIMEOUT);

  if(sessionTimeout<=dialogTimeout){
     throw new Error('Dialog timout must be less than session timout ');
  }

  const sessionTimeoutString = getCookie('session_timestamp')?.toString();
 
  useEffect(() => { 
     if(!sessionTimeoutString) { 
       return;
     }
    
    const sessionStartTime = parseInt(sessionTimeoutString as string);
    
    const createTimeout1 = () => setTimeout(()=>{   
      setWarningModalOpen(true);
    },(sessionTimeout * 1000))

    const createTimeout2 = () => setTimeout(() => { 
        logOffTheApp();
    },(dialogTimeout * 1000))

    const listener = () => {
      if(!isWarningModalOpen){
        const timeelapsed = Date.now()-sessionStartTime;
        const elapsedSeconds = timeelapsed / 1000; 
        if(elapsedSeconds > sessionTimeout)
        {
          //user been active but server session about to time out, extend session on server
          // extendSession(); well this thing went into loop
        }
        clearTimeout(timeout)
        timeout = createTimeout1();
      }
    } 

    // Initialization
    let timeout = isWarningModalOpen  ? createTimeout2() : createTimeout1()
    addEventListeners(listener);

    // Cleanup
    return () => {
      removeEventListeners(listener);
      clearTimeout(timeout);
    }
  },[isWarningModalOpen,logOffTheApp,sessionTimeout,dialogTimeout,sessionTimeoutString])

  return (
     <> 
      {isWarningModalOpen && (
        <TimeoutWarningModal 
         timoutInSeconds={dialogTimeout}
          isOpen={isWarningModalOpen}
          requestLogOffTheApp={logOffTheApp}
          requestUsingApp={continueUsingApp}
        />
        )
      }  
    </>
  ) 
}

interface SpanOfTime{
  h:number,
  m:number,
  s:number
}

interface TimeoutState{
  timeSpan:SpanOfTime,
  seconds:number 
}
const TimeoutWarningModal = ({ isOpen, requestLogOffTheApp, requestUsingApp,timoutInSeconds }:{
  isOpen:boolean,
  requestLogOffTheApp:LogOff
  requestUsingApp:()=>void
  timoutInSeconds: number
}) => {

  const emptyTimeSpan = {
    h: 0,
    m: 0,
    s: 0
  };
  const emptyState = {timeSpan:emptyTimeSpan,seconds:0};
  const [time, setTime] = useState<TimeoutState>({
     timeSpan: emptyTimeSpan, 
     seconds: timoutInSeconds 
    });
   
  const secondsToTime = (secs:number):SpanOfTime  => {
      const hours = Math.floor(secs / (60 * 60));

      const divisor_for_minutes = secs % (60 * 60);
      const minutes = Math.floor(divisor_for_minutes / 60);

      const divisor_for_seconds = divisor_for_minutes % 60;
      const seconds = Math.ceil(divisor_for_seconds);

      const obj = {
          h: hours,
          m: minutes,
          s: seconds
      };
      return obj;
  };
 
  useEffect(() => {
      // exit early when we reach 0
      if (!time || time.seconds<=0) return;
   
      // save intervalId to clear the interval when the
      // component re-renders
      const intervalId = setInterval(() => {
        // Remove one second, set state so a re-render happens.
          const seconds = time.seconds - 1;
          setTime({
              timeSpan: secondsToTime(seconds),
              seconds: seconds,
          });
      }, 1000);
  
      // clear interval on re-render to avoid memory leaks
      return () => clearInterval(intervalId);
      // add timeLeft as a dependency to re-rerun the effect
      // when we update it
    }, [time]);

  
  if (!isOpen) {
      setTime(emptyState);
  }
  if (time.timeSpan.s == 0) { 
    return <Fragment></Fragment>
  }
  return (
    <Dialog  open={isOpen} 
            onClose={()=>{ }}  className="relative z-10">
      <DialogBackdrop
        transition
        className="fixed inset-0 bg-gray-500/75 transition-opacity data-closed:opacity-0 data-enter:duration-300 data-enter:ease-out data-leave:duration-200 data-leave:ease-in"
      />

      <div className="fixed inset-0 z-10 w-screen overflow-y-auto">
        <div className="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
          <DialogPanel
            transition
            className="relative transform overflow-hidden rounded-lg bg-white px-4 pt-5 pb-4 text-left shadow-xl transition-all data-closed:translate-y-4 data-closed:opacity-0 data-enter:duration-300 data-enter:ease-out data-leave:duration-200 data-leave:ease-in sm:my-8 sm:w-full sm:max-w-lg sm:p-6 data-closed:sm:translate-y-0 data-closed:sm:scale-95"
          >
             
            <div className="sm:flex sm:items-start">
              <div className="mx-auto flex size-12 shrink-0 items-center justify-center rounded-full bg-red-100 sm:mx-0 sm:size-10">
                <ExclamationTriangleIcon aria-hidden="true" className="size-6 text-red-600" />
              </div>
              <div className="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
                <DialogTitle as="h3" className="text-base font-semibold text-gray-900">
                Session about to expire ({time.timeSpan.m.toString().padStart(2, "0")}:{time.timeSpan.s.toString().padStart(2, "0")})
                </DialogTitle>
                <div className="mt-2">
                  <p className="text-sm text-gray-500">
                    Do you want to stay logged in? 
                  </p>
                </div>
              </div>
            </div>
            <div className="mt-5 sm:mt-4 sm:flex sm:flex-row-reverse">
              <button
                type="button"
                onClick={()=>{
                  setTime(emptyState);
                  requestUsingApp(); 
              }}
                className="inline-flex w-full justify-center rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-blue-500 sm:ml-3 sm:w-auto"
              >
                Yes
              </button>
              <button
                type="button"
                onClick={requestLogOffTheApp}
                className="mt-3 inline-flex w-full justify-center rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 ring-1 shadow-xs ring-gray-300 ring-inset hover:bg-gray-50 sm:mt-0 sm:w-auto"
              >
                No
              </button>
            </div>
          </DialogPanel>
        </div>
      </div>
    </Dialog>
  )
   
}