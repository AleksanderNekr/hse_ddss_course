import os
import grpc
import HolidaysService
import Protos.calendars_pb2 as calendars
import Protos.calendars_pb2_grpc as calendars_grpc
from concurrent import futures

LISTEN_PORT = os.environ.get('LISTEN_PORT', '50051')


class HolidaysCalendarProvider(calendars_grpc.HolidaysProviderServicer):
    def GetHolidays(self, request, context):
        if request.country == calendars.RU:
            holidays = HolidaysService.parse_calendar_ru(request.year)
        elif request.country == calendars.ME:
            holidays = HolidaysService.parse_calendar_me(request.year)
        else:
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details("Unknown country")
            return calendars.HolidaysResponse()
        return calendars.HolidaysResponse(holidays=holidays["holidays"], pre_holidays=holidays["pre_holidays"])


def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    calendars_grpc.add_HolidaysProviderServicer_to_server(HolidaysCalendarProvider(), server)
    server.add_insecure_port('[::]:%s' % LISTEN_PORT)
    server.start()
    server.wait_for_termination()


if __name__ == '__main__':
    print('Starting server. Listening on port %s.' % LISTEN_PORT)
    try:
        serve()
    except Exception as e:
        print('Error: %s' % e)
        raise
