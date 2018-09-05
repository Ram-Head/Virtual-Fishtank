from flask import Flask, request, json
from queue import Queue

NUMBOXES = 2

connections = Queue()
disconnections = Queue()
accelQueues = [ Queue() for i in range(NUMBOXES) ]
SpecialQueues =  [ Queue() for i in range(NUMBOXES) ]
#connections.put('''{'secondBox': {'boxNo': 1, 'corner': 65, 'wall': 'bottom'}, 'timeStamp': -1, 'firstBox': {'boxNo': 1, 'corner': 65, 'wall': 'bottom'}}''')
#connections.put('''{'secondBox': {'boxNo': 1, 'corner': 65, 'wall': 'bottom'}, 'timeStamp': -1, 'firstBox': {'boxNo': 1, 'corner': 65, 'wall': 'bottom'}}''')
#disconnections.put('''{'secondBox': {'boxNo': 1, 'corner': 65, 'wall': 'bottom'}, 'timeStamp': -1, 'firstBox': {'boxNo': 1, 'corner': 65, 'wall': 'bottom'}}''')

class SensorDataType():
    CONNECTION = "connection"
    ACCEL = "accel"
    SPECIAL = "special"

app = Flask(__name__)
@app.route('/fishtank/dataInput/', methods=['GET', 'POST'])
def handleInput():
    error = None
    if request.method == 'POST':
        if request.headers['Content-Type'] == 'application/json':
            print(request.json)
            return handleSensorData(request.json)
        elif request.headers['Content-Type'] == 'text/plain':
            print(request.data)
            return "notification received"
        else:
            print("failed request")
            print(request)
            error = 'Invalid Format'
            return error
    return 'nothing to see here'

def handleSensorData(data):
    try:
        if data['type'] == SensorDataType.CONNECTION:
            if data['connect']:
                print('Card placed on reader')
                print(data['info'])
                connections.put(data['info'])
            else:
                print('Card removed from reader')
                disconnections.put(data['info'])
            return 'Success!'
        elif data['type'] == SensorDataType.ACCEL:
            accelQueues[data['boxNo']].put(data['info'])
            return 'success!'
        elif data['type'] == SensorDataType.SPECIAL:
            SpecialQueues[data['boxNo']].put(data['info'])
            return 'success!'
        else:
            return 'unrecognized data type'
    except:
        return 'Invalid JSON Format'

@app.route('/fishtank/connections', methods=['GET', 'POST'])
def handleConnections():
    print('*****************')
    if connections.empty():
        return 'empty'
    else:
        #switch to dumping a list of
        return json.dumps(connections.get())

@app.route('/fishtank/disconnections', methods=['GET', 'POST'])
def handleDiscnnections():
    if disconnections.empty():
        return 'empty'
    else:
        return json.dumps(disconnections.get())

@app.route('/fishtank/boxAccel/<boxNo>', methods=['GET', 'POST'])
def handleAccelOutput(boxNo):
    try:
        boxAccel = accelQueues[boxNo]
    except:
        return 'The provided box index is invalid'
    if boxAccel.empty():
        return 'empty'
    else:
        return json.dumps(boxAccel.get())

@app.route('/fishtank/boxSpecial/<boxNo>', methods=['GET', 'POST'])
def handleSpecialOutput(boxNo):
    try:
        boxSpecial = specialQueues[boxNo]
    except:
        return 'The provided box index is invalid'
    if boxSpecial.empty():
        return 'empty'
    else:
        return json.dumps(boxSpecial.get())

app.run(host='0.0.0.0', port=5000, debug = True)
